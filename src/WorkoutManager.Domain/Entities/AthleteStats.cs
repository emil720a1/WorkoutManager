using System;
using WorkoutManager.Domain.Common;

namespace WorkoutManager.Domain.Entities;

public class AthleteStats : Entity
{
    public Guid AthleteId { get; private set; }
    
    public int CurrentStreak { get; private set; }
    public int HighestStreak { get; private set; }
    public decimal TotalTonnage { get; private set; }

    private AthleteStats() : base(Guid.Empty) { }

    public AthleteStats(Guid id, Guid athleteId) : base(id)
    {
        AthleteId = athleteId;
        CurrentStreak = 0;
        HighestStreak = 0;
        TotalTonnage = 0m;
    }

    public void IncrementStreak()
    {
        CurrentStreak++;
        if (CurrentStreak > HighestStreak)
        {
            HighestStreak = CurrentStreak;
        }
    }

    public void ResetStreak()
    {
        CurrentStreak = 0;
    }

    public void AddTonnage(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("Tonnage cannot be negative", nameof(amount));
        TotalTonnage += amount;
    }
}
