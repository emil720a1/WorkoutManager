using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace WorkoutManager.BotGateway.Bot.Handlers;

public interface IBotUpdateHandler
{
    Task HandleUpdateAsync(Update update, CancellationToken cancellationToken);
    Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken);
}
