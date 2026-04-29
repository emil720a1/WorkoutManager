namespace WorkoutManager.Application.Interfaces;

using WorkoutManager.Application.Enums;

public interface IStateService
{
    AdminDialogState GetState(long telegramId);
    void SetState(long telegramId, AdminDialogState state);
    void ClearState(long telegramId);
}
