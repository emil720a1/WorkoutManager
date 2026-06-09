using System;
using System.Collections.Generic;

namespace WorkoutManager.Contracts;

public record WorkoutCreatedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public long AthleteId { get; init; }
    public int DayOfWeek { get; init; }
    public List<ExerciseDto> Exercises { get; init; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
