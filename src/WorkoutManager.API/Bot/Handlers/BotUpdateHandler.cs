using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using WorkoutManager.Application.Interfaces;

namespace WorkoutManager.API.Bot.Handlers;

public class BotUpdateHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<BotUpdateHandler> logger,
    ITelegramBotClient botClient) : IBotUpdateHandler
{
    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text is null)
            return;

        var message = update.Message;
        var chatId = message.Chat.Id;
        var text = message.Text;
        
        logger.LogInformation("Received message '{Text}' in chat {ChatId}.", text, chatId);

        await using var scope = scopeFactory.CreateAsyncScope();
        var workoutService = scope.ServiceProvider.GetRequiredService<IWorkoutService>();

        try 
        {
            if (text.StartsWith("/start"))
            {
                await HandleStartCommand(workoutService, message, cancellationToken);
            }
            else
            {
                await botClient.SendMessage(chatId, "Невідома команда. Спробуй /start.", cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Помилка при обробці команди {Text}", text);
            await botClient.SendMessage(chatId, "Сталася внутрішня помилка сервера.", cancellationToken: cancellationToken);
        }
    }

    public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error occurred during Telegram update handling.");
        return Task.CompletedTask;
    }

    private async Task HandleStartCommand(IWorkoutService workoutService, Message message, CancellationToken cancellationToken)
    {
        var telegramId = message.From?.Id ?? message.Chat.Id;
        var name = message.From?.FirstName ?? "Athlete";

        var result = await workoutService.RegisterAthleteAsync(telegramId, name);

        var replyText = result.IsSuccess 
            ? $"Привіт, {name}! Тебе успішно зареєстровано в системі WorkoutManager. Очікуй на призначення програми." 
            : $"З поверненням, {name}! Ти вже зареєстрований(-а).";

        await botClient.SendMessage(message.Chat.Id, replyText, cancellationToken: cancellationToken);
    }
}
