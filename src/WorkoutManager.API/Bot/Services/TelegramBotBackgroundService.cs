using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using WorkoutManager.Application.Common.Options;
using WorkoutManager.API.Bot.Handlers;

namespace WorkoutManager.API.Bot.Services;

public class TelegramBotBackgroundService(
    ITelegramBotClient botClient,
    IBotUpdateHandler botUpdateHandler,
    IOptions<BotConfiguration> botConfigOptions,
    ILogger<TelegramBotBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = botConfigOptions.Value;
        logger.LogInformation("Starting Telegram Bot Background Service...");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = []
        };

        try
        {
            await botClient.ReceiveAsync(
                updateHandler: async (client, update, ct) => await botUpdateHandler.HandleUpdateAsync(update, ct),
                errorHandler: async (client, exception, ct) => await botUpdateHandler.HandleErrorAsync(exception, ct),
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken
            );
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Telegram Bot crashed.");
        }
    }
}
