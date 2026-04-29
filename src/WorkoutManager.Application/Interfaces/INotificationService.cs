namespace WorkoutManager.Application.Interfaces;

using System.Threading;
using System.Threading.Tasks;

public interface INotificationService
{
    Task SendWorkoutReminderAsync(long telegramId, string message, CancellationToken ct);
}
