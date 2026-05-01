using System;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using WorkoutManager.Application.DTOs;
using WorkoutManager.Application.Enums;
using WorkoutManager.Application.Interfaces;

namespace WorkoutManager.Infrastructure.Services;

public class RedisStateService(IDistributedCache cache) : IStateService
{
    private readonly DistributedCacheEntryOptions _options = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(15)
    };

    public async Task<AdminDialogState> GetStateAsync(long telegramId)
    {
        var value = await cache.GetStringAsync($"WorkoutBot:State:{telegramId}");
        if (string.IsNullOrEmpty(value))
            return AdminDialogState.None;

        return Enum.TryParse<AdminDialogState>(value, out var state) ? state : AdminDialogState.None;
    }

    public async Task SetStateAsync(long telegramId, AdminDialogState state)
    {
        await cache.SetStringAsync($"WorkoutBot:State:{telegramId}", state.ToString(), _options);
    }

    public async Task ClearStateAsync(long telegramId)
    {
        await cache.RemoveAsync($"WorkoutBot:State:{telegramId}");
        await cache.RemoveAsync($"WorkoutBot:Draft:{telegramId}");
    }

    public async Task<WorkoutDraft?> GetDraftAsync(long telegramId)
    {
        var value = await cache.GetStringAsync($"WorkoutBot:Draft:{telegramId}");
        if (string.IsNullOrEmpty(value))
            return null;

        return JsonSerializer.Deserialize<WorkoutDraft>(value);
    }

    public async Task SetDraftAsync(long telegramId, WorkoutDraft draft)
    {
        var value = JsonSerializer.Serialize(draft);
        await cache.SetStringAsync($"WorkoutBot:Draft:{telegramId}", value, _options);
    }
}
