using WorkoutManager.Domain.Common;
using WorkoutManager.Domain.ValueObjects;

namespace WorkoutManager.Domain.Entities;

public class Exercise : Entity
{
    public string Name { get; private set; }
    public WorkoutVolume Volume { get; private set; }

    private Exercise() : base(Guid.Empty)
    {
        Name = null!;
        Volume = null!;
    }

    internal Exercise(string name, WorkoutVolume volume) : base(Guid.NewGuid())
    {
        Name = name;
        Volume = volume;
    }

    public static Exercise CreateParsed(string name, int sets, string reps)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Exercise name cannot be empty or whitespace.", nameof(name));
        }

        return new Exercise(name, new WorkoutVolume(sets, reps));
    }
}