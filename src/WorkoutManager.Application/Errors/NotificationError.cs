namespace WorkoutManager.Application.Errors;

public abstract record NotificationError
{
    private NotificationError() { }

    public sealed record DatabaseLookupFailed(string Reason) : NotificationError;
    public sealed record RedisPublishFailed(string Reason) : NotificationError;
    public sealed record NoWorkoutsForToday : NotificationError;
}
