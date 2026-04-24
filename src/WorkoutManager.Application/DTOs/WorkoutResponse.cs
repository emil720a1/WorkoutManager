namespace WorkoutManager.Application.DTOs;

public record WorkoutResponse(string Name, IReadOnlyCollection<ExerciseResponse> Exercises);
