using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using WorkoutManager.Application.Errors;
using WorkoutManager.Application.Interfaces;
using WorkoutManager.Domain.Entities;
using WorkoutManager.Domain.Interfaces;

namespace WorkoutManager.Application.Services;

/// <summary>
/// Orchestrates the Lazy Binding onboarding flow using the Result Pattern.
///
/// Instead of primitive `bool` returns or thrown exceptions, every failure path
/// is captured as a typed <see cref="OnboardingError"/>. The BotUpdateHandler
/// can then exhaustively switch on the error kind and send a tailored Telegram
/// reply — no try/catch needed at the controller level.
/// </summary>
public class AthleteOnboardingService(
    IAthleteRepository athleteRepository,
    IAthleteRedisRepository redisRepository,
    IApplicationDbContext dbContext,
    ILogger<AthleteOnboardingService> logger)
{
    // -----------------------------------------------------------------------
    // Step 1: Coach registers athlete by @username
    // Returns: Failure(AthleteAlreadyExists) if duplicate, Success otherwise.
    // -----------------------------------------------------------------------
    public async Task<UnitResult<OnboardingError>> RegisterPendingAthleteAsync(
        long coachTelegramId,
        string username,
        string displayName,
        CancellationToken ct = default)
    {
        var normalizedUsername = username.TrimStart('@').ToLowerInvariant();

        // Idempotency check — return typed failure instead of silent skip
        var existing = await athleteRepository.GetPendingByUsernameAsync(normalizedUsername, ct);
        if (existing is not null)
        {
            logger.LogInformation("Athlete @{Username} already registered. Skipping.", normalizedUsername);
            return UnitResult.Failure(
                (OnboardingError)new OnboardingError.AthleteAlreadyExists(normalizedUsername));
        }

        var athlete = new Athlete(telegramId: 0, name: displayName, username: normalizedUsername);
        await athleteRepository.AddAsync(athlete, ct);
        await dbContext.SaveChangesAsync(ct);

        await redisRepository.AddPendingAthleteAsync(coachTelegramId, normalizedUsername, ct);

        logger.LogInformation(
            "Coach {CoachId} registered pending athlete @{Username} (DB Id: {AthleteId})",
            coachTelegramId, normalizedUsername, athlete.Id);

        return UnitResult.Success<OnboardingError>();
    }

    // -----------------------------------------------------------------------
    // Step 2: Athlete sends /start — bind their real TelegramId
    // Returns: Failure(AthleteNotFound)       if username not in pending set.
    //          Failure(DomainValidationError)  if domain binding is rejected.
    //          Success                         on full completion.
    // -----------------------------------------------------------------------
    public async Task<UnitResult<OnboardingError>> TryBindAthleteAsync(
        long coachTelegramId,
        long athleteTelegramId,
        string username,
        CancellationToken ct = default)
    {
        var normalizedUsername = username.TrimStart('@').ToLowerInvariant();

        var athlete = await athleteRepository.GetPendingByUsernameAsync(normalizedUsername, ct);
        if (athlete is null)
        {
            logger.LogDebug("No pending athlete found for @{Username}. Nothing to bind.", normalizedUsername);
            return UnitResult.Failure(
                (OnboardingError)new OnboardingError.AthleteNotFound(normalizedUsername));
        }

        // Domain guard: BindTelegramId validates the state transition.
        // If the athlete is already Active (telegramId != 0), treat as a domain error.
        if (athlete.Status == AthleteStatus.Active)
        {
            return UnitResult.Failure(
                (OnboardingError)new OnboardingError.DomainValidationError(
                    $"Athlete @{normalizedUsername} is already active with TelegramId {athlete.TelegramId}."));
        }

        athlete.BindTelegramId(athleteTelegramId);
        athleteRepository.Update(athlete);
        await dbContext.SaveChangesAsync(ct);

        // Fire both Redis operations — they are independent, so run in parallel.
        await Task.WhenAll(
            redisRepository.BindAthleteAsync(coachTelegramId, normalizedUsername, athleteTelegramId, ct),
            redisRepository.CacheAthleteProfileAsync(athleteTelegramId, athlete.Name, normalizedUsername, ct));

        logger.LogInformation(
            "Lazy Binding complete: @{Username} bound to TelegramId {TelegramId}",
            normalizedUsername, athleteTelegramId);

        return UnitResult.Success<OnboardingError>();
    }
}
