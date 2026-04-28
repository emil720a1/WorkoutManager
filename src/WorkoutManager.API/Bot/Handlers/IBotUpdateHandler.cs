using Telegram.Bot.Types;

namespace WorkoutManager.API.Bot.Handlers;

public interface IBotUpdateHandler
{
    Task HandleUpdateAsync(Update update, CancellationToken cancellationToken);
    Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken);
}
