using WorkoutManager.Application.Common.Models;
using WorkoutManager.Application.DTOs;
using WorkoutManager.Application.Interfaces;
using WorkoutManager.Domain.Entities;
using WorkoutManager.Domain.Interfaces;

namespace WorkoutManager.Application.Services;

public class WorkoutService(IAthleteRepository athleteRepository, IProgramRepository programRepository) : IWorkoutService
{
    public async Task<Result<WorkoutResponse>> GetCurrentWorkoutAsync(long telegramId)
    {
        var athlete = await athleteRepository.GetByTelegramIdAsync(telegramId);
        if (athlete is null)
            return Result<WorkoutResponse>.Failure("Athlete not found.");

        if (athlete.CurrentProgramId is null)
            return Result<WorkoutResponse>.Failure("Athlete does not have an assigned program.");

        var program = await programRepository.GetByIdAsync(athlete.CurrentProgramId.Value);
        if (program is null)
            return Result<WorkoutResponse>.Failure("Program not found.");

        var today = DateTime.Today.DayOfWeek;
        var dayNumber = today is DayOfWeek.Sunday ? 7 : (int)today;

        var workoutDay = program.Days.FirstOrDefault(d => d.DayNumber == dayNumber);
        if (workoutDay is null)
            return Result<WorkoutResponse>.Failure("Rest day or no workout mapped for today.");

        var exerciseResponses = workoutDay.Exercises
            .Select(e => new ExerciseResponse(e.Name, e.Volume.Sets, e.Volume.Reps))
            .ToArray();

        var response = new WorkoutResponse(workoutDay.Name, [.. exerciseResponses]);
        
        return Result<WorkoutResponse>.Success(response);
    }

    public async Task<Result<bool>> RegisterAthleteAsync(long telegramId, string name)
    {
        var existingAthlete = await athleteRepository.GetByTelegramIdAsync(telegramId);
        if (existingAthlete is not null)
            return Result<bool>.Failure("Athlete is already registered.");

        var athlete = new Athlete(telegramId, name);
        await athleteRepository.AddAsync(athlete);
        
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> AssignProgramAsync(long telegramId, Guid programId)
    {
        var athlete = await athleteRepository.GetByTelegramIdAsync(telegramId);
        if (athlete is null)
            return Result<bool>.Failure("Athlete not found.");

        var program = await programRepository.GetByIdAsync(programId);
        if (program is null)
            return Result<bool>.Failure("Program not found.");

        athlete.SetProgram(program.Id);
        athleteRepository.Update(athlete);

        return Result<bool>.Success(true);
    }
}
