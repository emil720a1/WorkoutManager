namespace WorkoutManager.Application.Errors;

/// <summary>
/// Strongly-typed discriminated union for all onboarding business errors.
///
/// Using an abstract record with sealed subtypes gives exhaustive pattern matching
/// (switch expression warns on unhandled cases), and each error carries the
/// contextual data the caller needs to build a meaningful Telegram reply.
/// </summary>
public abstract record OnboardingError
{
    private OnboardingError() { } // prevents external inheritance

    /// <summary>Coach tried to add an athlete whose username is already tracked.</summary>
    public sealed record AthleteAlreadyExists(string Username) : OnboardingError
    {
        public override string ToString() =>
            $"Athlete @{Username} is already registered (pending or active).";
    }

    /// <summary>Athlete sent /start but their username is not in any pending set.</summary>
    public sealed record AthleteNotFound(string Username) : OnboardingError
    {
        public override string ToString() =>
            $"No pending athlete found for @{Username}. The coach must add them first.";
    }

    /// <summary>Domain entity rejected the operation (e.g. binding an already-active athlete).</summary>
    public sealed record DomainValidationError(string Reason) : OnboardingError
    {
        public override string ToString() => $"Domain validation failed: {Reason}";
    }
}
