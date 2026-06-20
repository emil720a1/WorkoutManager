using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using WorkoutManager.Contracts;

namespace WorkoutManager.BotGateway.Bot.Workers;

public class RedisNotificationSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<RedisNotificationSubscriber> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;

    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
    };

    public RedisNotificationSubscriber(
        IConnectionMultiplexer redis,
        ITelegramBotClient botClient,
        ILogger<RedisNotificationSubscriber> logger)
    {
        _redis = redis;
        _botClient = botClient;
        _logger = logger;

        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<ApiRequestException>(ex => ex.ErrorCode == 429 || ex.ErrorCode >= 500)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
            })
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();
        var channel = RedisChannel.Literal("workout:notifications");

        _logger.LogInformation("BotGateway listening on Redis channel: {Channel}", channel);

        var subscription = await subscriber.SubscribeAsync(channel);

        subscription.OnMessage(async message =>
        {
            try
            {
                var eventData = JsonSerializer.Deserialize<WorkoutNotificationEvent>(message.Message.ToString(), JsonOptions);
                if (eventData is null) return;

                string text = $"Good morning {eventData.AthleteName}! You have {eventData.Exercises.Count} exercises scheduled today. Let's go!\n\n";
                foreach (var ex in eventData.Exercises)
                {
                    text += $"- {ex.Name}: {ex.Sets} x {ex.Reps}\n";
                }
                
                await _resiliencePipeline.ExecuteAsync(async token =>
                {
                    await _botClient.SendMessage(
                        chatId: eventData.AthleteTelegramId,
                        text: text,
                        cancellationToken: token);
                }, stoppingToken);
                    
                _logger.LogInformation("Delivered morning notification to {TelegramId}", eventData.AthleteTelegramId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deliver Telegram message from Redis event after all retries.");
            }
        });

        // Keep the background service alive
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await subscriber.UnsubscribeAsync(channel);
    }
}
