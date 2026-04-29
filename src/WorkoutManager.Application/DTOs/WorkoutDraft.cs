namespace WorkoutManager.Application.DTOs;

public class WorkoutDraft
{
    public long AthleteId { get; set; }
    public int DayOfWeek { get; set; }
    public string? ExercisesText { get; set; }
}
