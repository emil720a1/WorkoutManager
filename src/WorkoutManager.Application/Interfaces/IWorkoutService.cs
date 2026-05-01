using WorkoutManager.Application.Common.Models;
using WorkoutManager.Application.DTOs;
using System.Collections.Generic;
using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Application.Interfaces;

public interface IWorkoutService
{
    Task<Result<WorkoutResponse>> GetCurrentWorkoutAsync(long telegramId);
    Task<Result<bool>> RegisterAthleteAsync(long telegramId, string name);
    Task<Result<bool>> AssignProgramAsync(long telegramId, Guid programId);
    
    Task<string> GetTodaysWorkoutAsync(long telegramId, CancellationToken cancellationToken);
    
    Task<Result<bool>> SaveParsedWorkoutAsync(long telegramId, int dayOfWeek, List<Exercise> exercises, CancellationToken cancellationToken);
}
