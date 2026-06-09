namespace WorkoutManager.Infrastructure.BackgroundJobs;

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkoutManager.Application.Interfaces;
using WorkoutManager.Contracts;

public class MorningWorkoutJob(
    IApplicationDbContext dbContext,
    IWorkoutService workoutService,
    IConnectionMultiplexer redis,
    ILogger<MorningWorkoutJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var currentDay = DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)DateTime.UtcNow.DayOfWeek;
        logger.LogInformation("Running MorningWorkoutJob for DayNumber {DayNumber}", currentDay);

        try
        {
            var programIds = await dbContext.Programs
                .AsNoTracking()
                .Where(p => p.Days.Any(d => d.DayNumber == currentDay))
                .Select(p => p.Id)
                .ToListAsync(ct);

            var telegramIds = await dbContext.Athletes
                .AsNoTracking()
                .Where(a => a.CurrentProgramId.HasValue && programIds.Contains(a.CurrentProgramId.Value))
                .Select(a => a.TelegramId)
                .ToListAsync(ct);

            var subscriber = redis.GetSubscriber();

            foreach (var telegramId in telegramIds)
            {
                string workoutText = await workoutService.GetTodaysWorkoutAsync(telegramId, ct);

                if (string.IsNullOrWhiteSpace(workoutText))
                {
                    logger.LogInformation("No specific workout found for user {TelegramId} today.", telegramId);
                    continue;
                }

                var command = new SendNotificationCommand
                {
                    TelegramId = telegramId,
                    MessageText = $"Привіт! Твоє тренування на сьогодні:\n\n{workoutText}"
                };

                var jsonPayload = JsonSerializer.Serialize(command);
                
                logger.LogInformation("Publishing SendNotificationCommand to notification_channel for User {TelegramId}", telegramId);
                
                await subscriber.PublishAsync(
                    RedisChannel.Literal("notification_channel"), 
                    new RedisValue(jsonPayload)
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute MorningWorkoutJob.");
            throw;
        }
    }
}
