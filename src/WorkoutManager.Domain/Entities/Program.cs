using WorkoutManager.Domain.Common;

namespace WorkoutManager.Domain.Entities;

public class Program : AggregateRoot
{
    public string Title { get; private set; }
    public string Description { get; private set; }

    private readonly List<WorkoutDay> _days = [];
    public IReadOnlyCollection<WorkoutDay> Days => _days.AsReadOnly();

    private Program() : base(Guid.Empty)
    {
        Title = null!;
        Description = null!;
    }

    public Program(string title, string description) : base(Guid.NewGuid())
    {
        Title = title;
        Description = description;
    }

    public void AddWorkoutDay(WorkoutDay day)
    {
        if (_days.Any(d => d.DayNumber == day.DayNumber))
            throw new InvalidOperationException($"Day {day.DayNumber} already exists in this program.");
        
        _days.Add(day);
    }
}