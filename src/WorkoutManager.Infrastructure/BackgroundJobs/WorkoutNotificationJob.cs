using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkoutManager.Application.Errors;
using WorkoutManager.Application.Interfaces;
using WorkoutManager.Contracts;
using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Infrastructure.BackgroundJobs;

public class WorkoutNotificationJob(
    IApplicationDbContext dbContext,
    IConnectionMultiplexer redis,
    ILogger<WorkoutNotificationJob> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
    };

    /// <summary>
    /// Registers the Hangfire Recurring Job. Called during application startup.
    /// </summary>
    public void ScheduleDailyNotificationsJob()
    {
        RecurringJob.AddOrUpdate(
            "daily-workout-notifications",
            () => ProcessDailyNotificationsAsync(CancellationToken.None),
            "0 7 * * *"); // 07:00 AM Daily
    }

    /// <summary>
    /// Executes the daily notification batch.
    /// </summary>
    public async Task<UnitResult<NotificationError>> ProcessDailyNotificationsAsync(CancellationToken ct)
    {
        try
        {
            // Note: In a production global app, consider the user's specific TimeZone.
            var currentDay = (int)DateTime.UtcNow.DayOfWeek;
            currentDay = currentDay == 0 ? 7 : currentDay; // Normalize Sunday from 0 to 7

            // Highly optimized projection: No entity tracking, fetching only required fields.
            var pendingNotifications = await dbContext.Athletes
                .AsNoTracking()
                .Where(a => a.Status == AthleteStatus.Active && a.CurrentProgramId.HasValue)
                .SelectMany(a => dbContext.Programs
                    .Where(p => p.Id == a.CurrentProgramId)
                    .SelectMany(p => p.Days.Where(d => d.DayNumber == currentDay))
                    .Select(d => new WorkoutNotificationEvent(
                        a.TelegramId,
                        a.Name,
                        d.Exercises.Select(e => new ExerciseNotificationDto(
                            e.Name, 
                            e.Volume.Sets, 
                            e.Volume.Reps)).ToList())))
                .ToListAsync(ct);

            if (pendingNotifications.Count == 0)
            {
                logger.LogInformation("No active workouts found for day {DayNumber}", currentDay);
                return UnitResult.Failure((NotificationError)new NotificationError.NoWorkoutsForToday());
            }
            var subscriber = redis.GetSubscriber();
            var channel = RedisChannel.Literal("workout:notifications");

            foreach (var notification in pendingNotifications)
            {
                var payload = JsonSerializer.Serialize(notification, JsonOptions);
                
                await subscriber.PublishAsync(channel, new RedisValue(payload));
                
                logger.LogInformation("Published workout notification for Athlete {TelegramId}", notification.AthleteTelegramId);
            }

            return UnitResult.Success<NotificationError>();
        }
        catch (RedisException ex)
        {
            logger.LogError(ex, "Redis Pub/Sub failed during notification broadcast.");
            return UnitResult.Failure((NotificationError)new NotificationError.RedisPublishFailed(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database lookup failed during notification generation.");
            return UnitResult.Failure((NotificationError)new NotificationError.DatabaseLookupFailed(ex.Message));
        }
    }
}
