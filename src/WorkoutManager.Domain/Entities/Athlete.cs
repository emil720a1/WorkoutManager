using WorkoutManager.Domain.Common;

namespace WorkoutManager.Domain.Entities;

public class Athlete : AggregateRoot
{
    public long TelegramId { get; private set; }
    public string Name { get; private set; }
    public Guid? CurrentProgramId { get; private set; }
    public TimeOnly NotificationTime { get; private set; }

    public Athlete(long telegramId, string name) : base(Guid.NewGuid())
    {
        TelegramId = telegramId;
        Name = name;
        NotificationTime = new TimeOnly(7, 0);
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