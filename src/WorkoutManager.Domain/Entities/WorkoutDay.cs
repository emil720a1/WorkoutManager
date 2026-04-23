using WorkoutManager.Domain.Common;
using WorkoutManager.Domain.ValueObjects;

namespace WorkoutManager.Domain.Entities;

public class WorkoutDay : Entity
{
    public int DayNumber { get; private set; }
    public string Name { get; private set; }
    
    private readonly List<Exercise> _exercises = new();
    public IReadOnlyCollection<Exercise> Exercises => _exercises.AsReadOnly();

    public WorkoutDay(int dayNumber, string name) : base(Guid.NewGuid())
    {
        DayNumber = dayNumber;
        Name = name;
    }

    public void AddExercise(string name, int sets, string reps)
    {
        var volume = new WorkoutVolume(sets, reps);
        _exercises.Add(new Exercise(name, volume));
    }
}