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

    public AdminDialogState GetState(long telegramId)
    {
        var value = cache.GetString($"WorkoutBot:State:{telegramId}");
        if (string.IsNullOrEmpty(value))
            return AdminDialogState.None;

        return Enum.TryParse<AdminDialogState>(value, out var state) ? state : AdminDialogState.None;
    }

    public void SetState(long telegramId, AdminDialogState state)
    {
        cache.SetString($"WorkoutBot:State:{telegramId}", state.ToString(), _options);
    }

    public void ClearState(long telegramId)
    {
        cache.Remove($"WorkoutBot:State:{telegramId}");
        cache.Remove($"WorkoutBot:Draft:{telegramId}");
    }

    public WorkoutDraft? GetDraft(long telegramId)
    {
        var value = cache.GetString($"WorkoutBot:Draft:{telegramId}");
        if (string.IsNullOrEmpty(value))
            return null;

        return JsonSerializer.Deserialize<WorkoutDraft>(value);
    }

    public void SetDraft(long telegramId, WorkoutDraft draft)
    {
        var value = JsonSerializer.Serialize(draft);
        cache.SetString($"WorkoutBot:Draft:{telegramId}", value, _options);
    }
}
