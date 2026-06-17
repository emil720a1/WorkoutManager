namespace WorkoutManager.Domain.Interfaces;

/// <summary>
/// Redis-based operations for athlete onboarding and fast coach dashboard lookups.
/// </summary>
public interface IAthleteRedisRepository
{
    // Step 1 (Coach adds athlete by username): track as pending
    Task AddPendingAthleteAsync(long coachId, string username, CancellationToken ct = default);

    // Step 2 (Athlete sends /start): promote from pending → active
    Task BindAthleteAsync(long coachId, string username, long athleteTelegramId, CancellationToken ct = default);

    // Step 3 (Coach dashboard): get active student IDs
    Task<IReadOnlyList<long>> GetActiveAthleteIdsAsync(long coachId, CancellationToken ct = default);

    // Step 3 (Dynamic menu): get athlete profile (name + username) by telegram id
    Task<(string Name, string Username)?> GetAthleteProfileAsync(long athleteTelegramId, CancellationToken ct = default);

    // Called after BindTelegramId to cache profile for fast menu generation
    Task CacheAthleteProfileAsync(long athleteTelegramId, string name, string username, CancellationToken ct = default);
}
