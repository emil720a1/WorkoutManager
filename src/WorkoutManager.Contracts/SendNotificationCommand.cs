using System;

namespace WorkoutManager.Contracts;

public record SendNotificationCommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
    public long TelegramId { get; init; }
    public string MessageText { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
