using Telegram.Bot.Types.ReplyMarkups;

namespace WorkoutManager.BotGateway.Bot.Helpers;

/// <summary>
/// Abstraction over Redis athlete cache — keeps BotGateway decoupled from Domain/Infrastructure.
/// Implemented in BotGateway itself using IConnectionMultiplexer.
/// </summary>
public interface IActiveAthleteCache
{
    Task<IReadOnlyList<long>> GetActiveAthleteIdsAsync(long coachId, CancellationToken ct = default);
    Task<(string Name, string Username)?> GetAthleteProfileAsync(long athleteTelegramId, CancellationToken ct = default);
}

/// <summary>
/// Builds an InlineKeyboardMarkup with buttons for each active athlete.
/// Data is fetched from Redis — O(1) profile lookups, no DB round-trips.
/// </summary>
public sealed class AthleteMenuBuilder(IActiveAthleteCache cache)
{
    /// <summary>
    /// Fetches active athlete IDs from Redis SET coach:{coachId}:students,
    /// retrieves each athlete's name from their Redis HASH user:{id}:profile,
    /// and builds an inline keyboard where:
    ///   - Button text  = athlete display name
    ///   - CallbackData = "select_student:{telegramId}"
    /// </summary>
    public async Task<InlineKeyboardMarkup> BuildStudentListAsync(
        long coachId,
        CancellationToken ct = default)
    {
        var athleteIds = await cache.GetActiveAthleteIdsAsync(coachId, ct);

        if (athleteIds.Count == 0)
        {
            return new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("No active students yet", "noop"));
        }

        var profileTasks = athleteIds
            .Select(async id =>
            {
                var profile = await cache.GetAthleteProfileAsync(id, ct);
                return (Id: id, Name: profile?.Name ?? $"Student #{id}");
            })
            .ToList();

        var profiles = await Task.WhenAll(profileTasks);

        var buttons = profiles
            .Select(p => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: p.Name,
                    callbackData: $"select_student:{p.Id}")
            })
            .ToArray();

        return new InlineKeyboardMarkup(buttons);
    }
}
