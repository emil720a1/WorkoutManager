using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Telegram.Bot;
using WorkoutManager.Contracts;

namespace WorkoutManager.API.Bot.Workers;

public class NotificationSubscriberWorker : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<NotificationSubscriberWorker> _logger;
    private ChannelMessageQueue? _channelSubscription;

    public NotificationSubscriberWorker(
        IConnectionMultiplexer redis,
        ITelegramBotClient botClient,
        ILogger<NotificationSubscriberWorker> logger)
    {
        _redis = redis;
        _botClient = botClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationSubscriberWorker is starting and subscribing to Redis...");

        var subscriber = _redis.GetSubscriber();
        _channelSubscription = await subscriber.SubscribeAsync(RedisChannel.Literal("notification_channel"));

        _channelSubscription.OnMessage(async message =>
        {
            try
            {
                var rawJson = message.Message.ToString();
                _logger.LogInformation("New notification message received from broker.");

                var command = JsonSerializer.Deserialize<SendNotificationCommand>(rawJson);

                if (command == null)
                {
                    _logger.LogWarning("Failed to deserialize SendNotificationCommand. Payload was empty or corrupted.");
                    return;
                }

                _logger.LogInformation("Sending message via Telegram API to user {TelegramId} (Command: {CommandId})", 
                    command.TelegramId, command.CommandId);

                await _botClient.SendMessage(
                    chatId: command.TelegramId,
                    text: command.MessageText,
                    cancellationToken: stoppingToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deliver message via Telegram client in NotificationSubscriberWorker.");
            }
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await subscriber.UnsubscribeAsync(RedisChannel.Literal("notification_channel"));
        _logger.LogInformation("NotificationSubscriberWorker has successfully unsubscribed.");
    }
}
