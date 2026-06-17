using WorkoutManager.Domain.Common;

namespace WorkoutManager.Domain.Entities;

public enum AthleteStatus { Pending, Active }

public class Athlete : AggregateRoot
{
    public long TelegramId { get; private set; }
    public string Username { get; private set; }
    public string Name { get; private set; }
    public AthleteStatus Status { get; private set; }
    public Guid? CurrentProgramId { get; private set; }
    public TimeOnly NotificationTime { get; private set; }

    private Athlete() : base(Guid.Empty)
    {
        Name = null!;
        Username = null!;
    }

    // Constructor for an already-known athlete (TelegramId is known)
    public Athlete(long telegramId, string name, string username = "") : base(Guid.NewGuid())
    {
        TelegramId = telegramId;
        Name = name;
        Username = username.TrimStart('@').ToLowerInvariant();
        Status = telegramId == 0 ? AthleteStatus.Pending : AthleteStatus.Active;
        NotificationTime = new TimeOnly(7, 0);
    }

    // Lazy Binding: called when athlete sends /start and we match by username
    public void BindTelegramId(long telegramId)
    {
        TelegramId = telegramId;
        Status = AthleteStatus.Active;
    }

    public void SetProgram(Guid programId)
    {
        CurrentProgramId = programId;
    }

    public void UpdateNotificationTime(TimeOnly newTime)
    {
        NotificationTime = newTime;
    }
}