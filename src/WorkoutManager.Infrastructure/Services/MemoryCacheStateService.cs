namespace WorkoutManager.Infrastructure.Services;

using System;
using Microsoft.Extensions.Caching.Memory;
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
    }
}
