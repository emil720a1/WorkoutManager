namespace WorkoutManager.Application.Interfaces;

using WorkoutManager.Application.DTOs;
using WorkoutManager.Application.Enums;

public interface IStateService
{
    AdminDialogState GetState(long telegramId);
    void SetState(long telegramId, AdminDialogState state);
    void ClearState(long telegramId);
    WorkoutDraft? GetDraft(long telegramId);
    void SetDraft(long telegramId, WorkoutDraft draft);
}
