using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using WorkoutManager.BotGateway.Bot.Helpers;

namespace WorkoutManager.BotGateway.Bot.Services;

public class RedisActiveAthleteCache(IConnectionMultiplexer redis) : IActiveAthleteCache
{
    public async Task<IReadOnlyList<long>> GetActiveAthleteIdsAsync(long coachId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var members = await db.SetMembersAsync($"coach:{coachId}:students");
        
        return members
            .Select(m => (long)m)
            .ToList();
    }

    public async Task<(string Name, string Username)?> GetAthleteProfileAsync(long athleteTelegramId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var hash = await db.HashGetAllAsync($"user:{athleteTelegramId}:profile");

        if (hash.Length == 0)
            return null;

        var name = hash.FirstOrDefault(h => h.Name == "Name").Value.ToString();
        var username = hash.FirstOrDefault(h => h.Name == "Username").Value.ToString();

        return (name ?? $"User_{athleteTelegramId}", username ?? string.Empty);
    }
}
