using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Telegram.Bot;
using WorkoutManager.Contracts;

namespace WorkoutManager.BotGateway.Bot.Workers;

public class RedisNotificationSubscriber(
    IConnectionMultiplexer redis,
    ITelegramBotClient botClient,
    ILogger<RedisNotificationSubscriber> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = redis.GetSubscriber();
        var channel = RedisChannel.Literal("workout:notifications");

        logger.LogInformation("BotGateway listening on Redis channel: {Channel}", channel);

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
                
                await botClient.SendMessage(
                    chatId: eventData.AthleteTelegramId,
                    text: text,
                    cancellationToken: stoppingToken);
                    
                logger.LogInformation("Delivered morning notification to {TelegramId}", eventData.AthleteTelegramId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deliver Telegram message from Redis event.");
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
