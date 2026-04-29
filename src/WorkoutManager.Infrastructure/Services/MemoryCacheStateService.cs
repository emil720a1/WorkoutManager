namespace WorkoutManager.Infrastructure.Services;

using System;
using Microsoft.Extensions.Caching.Memory;
using WorkoutManager.Application.DTOs;
using WorkoutManager.Application.Enums;
using WorkoutManager.Application.Interfaces;

public class MemoryCacheStateService(IMemoryCache cache) : IStateService
{
    public AdminDialogState GetState(long telegramId)
    {
        return cache.TryGetValue($"admin_state_{telegramId}", out AdminDialogState state) 
            ? state 
            : AdminDialogState.None;
    }

    public void SetState(long telegramId, AdminDialogState state)
    {
        cache.Set($"admin_state_{telegramId}", state, TimeSpan.FromMinutes(15));
    }

    public void ClearState(long telegramId)
    {
        cache.Remove($"admin_state_{telegramId}");
        cache.Remove($"draft_{telegramId}");
    }

    public WorkoutDraft? GetDraft(long telegramId)
    {
        return cache.TryGetValue($"draft_{telegramId}", out WorkoutDraft? draft) ? draft : null;
    }

    public void SetDraft(long telegramId, WorkoutDraft draft)
    {
        cache.Set($"draft_{telegramId}", draft, TimeSpan.FromMinutes(15));
    }
}
