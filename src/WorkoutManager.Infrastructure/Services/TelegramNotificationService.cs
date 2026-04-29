namespace WorkoutManager.Infrastructure.Services;

using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using WorkoutManager.Application.Interfaces;

public class TelegramNotificationService(ITelegramBotClient botClient) : INotificationService
{
    public async Task SendWorkoutReminderAsync(long telegramId, string message, CancellationToken ct)
    {
        await botClient.SendMessage(telegramId, message, cancellationToken: ct);
    }
}
