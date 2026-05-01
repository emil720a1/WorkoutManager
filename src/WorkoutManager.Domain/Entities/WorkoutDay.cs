using WorkoutManager.Domain.Common;
using WorkoutManager.Domain.ValueObjects;

namespace WorkoutManager.Domain.Entities;

public class WorkoutDay : Entity
{
    public int DayNumber { get; private set; }
    public string Name { get; private set; }
    
    private readonly List<Exercise> _exercises = [];
    public IReadOnlyCollection<Exercise> Exercises => _exercises.AsReadOnly();

    private WorkoutDay() : base(Guid.Empty)
    {
        Name = null!;
    }

    public WorkoutDay(int dayNumber, string name) : base(Guid.NewGuid())
    {
        DayNumber = dayNumber;
        Name = name;
    }

    public void AddExercise(string name, int sets, string reps)
    {
        var volume = new WorkoutVolume(sets, reps);
        AddExercise(new Exercise(name, volume));
    }

    public void AddExercise(Exercise exercise)
    {
        _exercises.Add(exercise);
    }

    public void ClearExercises()
    {
        _exercises.Clear();
    }
}