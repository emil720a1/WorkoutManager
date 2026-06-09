using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkoutManager.Contracts;
using WorkoutManager.Application.Interfaces;
using WorkoutManager.Domain.Entities;
using WorkoutManager.Domain.ValueObjects;

namespace WorkoutManager.Infrastructure.Workers;

public class RedisSubscriberWorker : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RedisSubscriberWorker> _logger;
    private ChannelMessageQueue? _channelSubscription;

    public RedisSubscriberWorker(
        IConnectionMultiplexer redis,
        IServiceScopeFactory scopeFactory,
        ILogger<RedisSubscriberWorker> logger)
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RedisSubscriberWorker is starting and subscribing to 'workout_channel'...");

        var subscriber = _redis.GetSubscriber();
        _channelSubscription = await subscriber.SubscribeAsync(RedisChannel.Literal("workout_channel"));

        _channelSubscription.OnMessage(async message =>
        {
            try
            {
                var rawJson = message.Message.ToString();
                _logger.LogInformation("New workout integration event received.");

                var workoutEvent = JsonSerializer.Deserialize<WorkoutCreatedEvent>(rawJson);

                if (workoutEvent == null)
                {
                    _logger.LogWarning("Deserialized WorkoutCreatedEvent was null.");
                    return;
                }

                await ProcessWorkoutCreationAsync(workoutEvent, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error inside subscriber loop of RedisSubscriberWorker.");
            }
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await subscriber.UnsubscribeAsync(RedisChannel.Literal("workout_channel"));
        _logger.LogInformation("RedisSubscriberWorker has successfully unsubscribed.");
    }

    private async Task ProcessWorkoutCreationAsync(WorkoutCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing WorkoutCreatedEvent {EventId} for Athlete {AthleteId}", @event.EventId, @event.AthleteId);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var workoutService = scope.ServiceProvider.GetRequiredService<IWorkoutService>();

        try
        {
            var domainExercises = new List<Exercise>();

            foreach (var dto in @event.Exercises)
            {
                var volume = new WorkoutVolume(dto.Sets, dto.Reps);

                var exerciseInstance = (Exercise)Activator.CreateInstance(
                    typeof(Exercise),
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    [dto.Name, volume],
                    null
                )!;

                domainExercises.Add(exerciseInstance);
            }

            var saveResult = await workoutService.SaveParsedWorkoutAsync(
                @event.AthleteId,
                @event.DayOfWeek,
                domainExercises,
                cancellationToken
            );

            if (saveResult.IsSuccess)
            {
                _logger.LogInformation("Successfully saved integration workout for Event {EventId}.", @event.EventId);
            }
            else
            {
                _logger.LogError("Business logic rejected saving workout for Event {EventId}. Reason: {Error}", 
                    @event.EventId, saveResult.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute database persistence for Event {EventId} in subscriber background service.", @event.EventId);
        }
    }
}
