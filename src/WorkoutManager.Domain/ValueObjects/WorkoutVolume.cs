namespace WorkoutManager.Domain.ValueObjects;

public record WorkoutVolume
{
    public int Sets { get; init; }
    public string Reps { get; init; }

    private WorkoutVolume()
    {
        Reps = null!;
    }

    public WorkoutVolume(int sets, string reps)
    {
        Sets = sets;
        Reps = reps;
    }
}