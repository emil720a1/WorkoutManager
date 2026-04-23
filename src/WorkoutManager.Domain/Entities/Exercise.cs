using WorkoutManager.Domain.Common;
using WorkoutManager.Domain.ValueObjects;

namespace WorkoutManager.Domain.Entities;

public class Exercise : Entity
{
    public string Name { get; private set; }
    public WorkoutVolume Volume { get; private set; }

    internal Exercise(string name, WorkoutVolume volume) : base(Guid.NewGuid())
    {
        Name = name;
        Volume = volume;
    }
}