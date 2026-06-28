using System;
using WorkoutManager.Domain.Common;

namespace WorkoutManager.Domain.Entities;

public class Workout : Entity
{
    public Guid AthleteId { get; private set; }
    public Guid WorkoutDayId { get; private set; }
    
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private Workout() : base(Guid.Empty) { }

    public Workout(Guid id, Guid athleteId, Guid workoutDayId) : base(id)
    {
        AthleteId = athleteId;
        WorkoutDayId = workoutDayId;
        IsCompleted = false;
    }

    public void MarkAsCompleted()
    {
        if (IsCompleted) return;
        
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
    }

    public decimal CalculateTotalTonnage()
    {
        // TODO: iterate over logged sets/reps to calculate tonnage
        return 0;
    }
}
