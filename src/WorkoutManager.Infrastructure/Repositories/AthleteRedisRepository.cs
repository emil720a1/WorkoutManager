using StackExchange.Redis;
using WorkoutManager.Domain.Interfaces;

namespace WorkoutManager.Infrastructure.Repositories;

/// <summary>
/// High-performance Redis repository for athlete onboarding and coach dashboard.
///
/// Key schema:
///   coach:{coachId}:pending_students  — SET of usernames (lowercase strings)
///   coach:{coachId}:students          — SET of athlete Telegram IDs (stored as strings)
///   user:{telegramId}:profile         — HASH { "name": string, "username": string }
/// </summary>
public sealed class AthleteRedisRepository(IConnectionMultiplexer redis) : IAthleteRedisRepository
{
    // GetDatabase() is O(1) and thread-safe — safe to cache at construction time.
    private readonly IDatabase _db = redis.GetDatabase();

    // Static key builders avoid per-call allocations from captured closures.
    private static string PendingKey(long coachId)  => $"coach:{coachId}:pending_students";
    private static string ActiveKey(long coachId)   => $"coach:{coachId}:students";
    private static string ProfileKey(long telegramId) => $"user:{telegramId}:profile";

    // Pre-allocated field names reused on every GetAthleteProfileAsync call.
    // Prevents repeated RedisValue boxing of the same strings.
    private static readonly RedisValue[] ProfileFields = ["name", "username"];

    // -----------------------------------------------------------------------
    // Step 1: Coach registers an athlete by @username
    // -----------------------------------------------------------------------
    public async Task AddPendingAthleteAsync(
        long coachId,
        string username,
        CancellationToken ct = default)
    {
        // Normalize exactly once — no repeated allocations downstream.
        var normalized = username.ToLowerInvariant();
        await _db.SetAddAsync(PendingKey(coachId), normalized);
    }

    // -----------------------------------------------------------------------
    // Step 2: Athlete sends /start — bind their real TelegramId atomically
    // -----------------------------------------------------------------------
    public async Task BindAthleteAsync(
        long coachId,
        string username,
        long athleteTelegramId,
        CancellationToken ct = default)
    {
        var normalized = username.ToLowerInvariant();

        // Use CreateTransaction() (MULTI/EXEC) for true atomicity.
        // IBatch.Execute() is fire-and-forget and does NOT guarantee atomicity.
        // With a transaction, Redis executes both commands as one indivisible unit.
        var tran = _db.CreateTransaction();

        // Queue commands — do NOT await inside a transaction block.
        // Awaiting inside a transaction causes a deadlock because the commands
        // are only sent to Redis after Execute() is called.
        var removeTask = tran.SetRemoveAsync(PendingKey(coachId), normalized);
        var addTask    = tran.SetAddAsync(ActiveKey(coachId), athleteTelegramId.ToString());

        // Execute sends MULTI + queued commands + EXEC as a single pipeline.
        await tran.ExecuteAsync();

        // Awaiting here is safe — the tasks are already completed after ExecuteAsync.
        await Task.WhenAll(removeTask, addTask);
    }

    // -----------------------------------------------------------------------
    // Step 3: Coach dashboard — fetch all active athlete IDs
    // -----------------------------------------------------------------------
    public async Task<IReadOnlyList<long>> GetActiveAthleteIdsAsync(
        long coachId,
        CancellationToken ct = default)
    {
        var members = await _db.SetMembersAsync(ActiveKey(coachId));

        // Fast-path: avoid heap allocation for empty sets.
        if (members.Length == 0)
            return Array.Empty<long>();

        // Pre-allocate with exact capacity — eliminates List<T> internal resizing.
        var result = new List<long>(capacity: members.Length);

        foreach (var member in members)
        {
            // Explicit string cast resolves the ambiguous overload between
            // TryParse(ReadOnlySpan<byte>, out long) and TryParse(string?, out long).
            if (long.TryParse((string?)member, out var id))
                result.Add(id);
        }

        return result;
    }

    // -----------------------------------------------------------------------
    // Cache: store athlete profile for fast InlineKeyboard generation
    // -----------------------------------------------------------------------
    public async Task CacheAthleteProfileAsync(
        long athleteTelegramId,
        string name,
        string username,
        CancellationToken ct = default)
    {
        var key = ProfileKey(athleteTelegramId);

        // Use IBatch (pipeline) to send HashSet + KeyExpire in a single
        // network roundtrip instead of two sequential awaited calls.
        var batch = _db.CreateBatch();

        var hashTask   = batch.HashSetAsync(key, [
            new HashEntry("name",     name),
            new HashEntry("username", username)
        ]);
        var expireTask = batch.KeyExpireAsync(key, TimeSpan.FromDays(30));

        // Execute() flushes the pipeline — both commands sent together.
        batch.Execute();

        await Task.WhenAll(hashTask, expireTask);
    }

    // -----------------------------------------------------------------------
    // Cache: read athlete profile (zero-allocation field targeting)
    // -----------------------------------------------------------------------
    public async Task<(string Name, string Username)?> GetAthleteProfileAsync(
        long athleteTelegramId,
        CancellationToken ct = default)
    {
        // HashGetAsync with explicit fields requests ONLY "name" and "username"
        // via a single HMGET command — avoids fetching the entire hash with
        // HGETALL and eliminates the Dictionary<> allocation of HashGetAllAsync.
        var values = await _db.HashGetAsync(ProfileKey(athleteTelegramId), ProfileFields);

        // values[0] = "name", values[1] = "username"
        // HasValue guards against keys that expired or were never written.
        if (!values[0].HasValue || !values[1].HasValue)
            return null;

        return ((string)values[0]!, (string)values[1]!);
    }
}
