namespace WorkoutManager.Application.Interfaces;

using WorkoutManager.Application.DTOs;
using WorkoutManager.Application.Enums;

public interface IStateService
{
    Task<AdminDialogState> GetStateAsync(long telegramId);
    Task SetStateAsync(long telegramId, AdminDialogState state);
    Task ClearStateAsync(long telegramId);
    Task<WorkoutDraft?> GetDraftAsync(long telegramId);
    Task SetDraftAsync(long telegramId, WorkoutDraft draft);
}
