using WorkoutManager.Application.Common.Models;
using WorkoutManager.Application.DTOs;

namespace WorkoutManager.Application.Interfaces;

public interface IWorkoutService
{
    Task<Result<WorkoutResponse>> GetCurrentWorkoutAsync(long telegramId);
    Task<Result<bool>> RegisterAthleteAsync(long telegramId, string name);
    Task<Result<bool>> AssignProgramAsync(long telegramId, Guid programId);
}
