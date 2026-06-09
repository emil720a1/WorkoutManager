using WorkoutManager.BotGateway.Bot.DTOs;
using WorkoutManager.BotGateway.Bot.Enums;

namespace WorkoutManager.BotGateway.Bot.Interfaces;

public interface IStateService
{
    AdminDialogState GetState(long telegramId);
    void SetState(long telegramId, AdminDialogState state);
    void ClearState(long telegramId);
    WorkoutDraft? GetDraft(long telegramId);
    void SetDraft(long telegramId, WorkoutDraft draft);
}
