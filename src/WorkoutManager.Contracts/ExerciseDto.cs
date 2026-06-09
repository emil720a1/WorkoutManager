namespace WorkoutManager.Contracts;

public record ExerciseDto
{
    public string Name { get; init; } = string.Empty;
    public int Sets { get; init; }
    public string Reps { get; init; } = string.Empty;
}
