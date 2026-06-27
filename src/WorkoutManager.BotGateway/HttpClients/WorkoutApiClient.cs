using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using WorkoutManager.Contracts;

namespace WorkoutManager.BotGateway.HttpClients;

public class WorkoutApiClient(HttpClient httpClient, ILogger<WorkoutApiClient> logger)
{
    public async Task<UnitResult<string>> AssignWorkoutAsync(long athleteTelegramId, IEnumerable<ExerciseDto> exercises, CancellationToken ct)
    {
        try
        {
            var request = new AssignWorkoutRequestDto(athleteTelegramId, exercises);
            var response = await httpClient.PostAsJsonAsync("/api/v1/workouts", request, ct);
            
            if (response.IsSuccessStatusCode)
                return UnitResult.Success<string>();
            
            var error = await response.Content.ReadAsStringAsync(ct);
            return UnitResult.Failure<string>(error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HTTP request to assign workout failed");
            return UnitResult.Failure<string>("Network error occurred while assigning workout.");
        }
    }

    public async Task<UnitResult<string>> OnboardAthleteAsync(long telegramId, string username, CancellationToken ct)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/v1/athletes/onboard", new { AthleteTelegramId = telegramId, Username = username }, ct);
            
            if (response.IsSuccessStatusCode)
                return UnitResult.Success<string>();
            
            var error = await response.Content.ReadAsStringAsync(ct);
            return UnitResult.Failure<string>(error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HTTP request to onboard athlete failed");
            return UnitResult.Failure<string>("Network error occurred while onboarding.");
        }
    }

    public async Task<Result<WorkoutResponseDto, string>> GetTodayWorkoutAsync(long telegramId, CancellationToken ct)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/v1/workouts/today?telegramId={telegramId}", ct);
            
            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var workout = await response.Content.ReadFromJsonAsync<WorkoutResponseDto>(options, cancellationToken: ct);
                
                return workout is not null 
                    ? Result.Success<WorkoutResponseDto, string>(workout) 
                    : Result.Failure<WorkoutResponseDto, string>("Failed to parse response.");
            }
            
            var error = await response.Content.ReadAsStringAsync(ct);
            return Result.Failure<WorkoutResponseDto, string>(error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HTTP request to fetch today's workout failed");
            return Result.Failure<WorkoutResponseDto, string>("Network error occurred while fetching workout.");
        }
    }
}

public record AssignWorkoutRequestDto(long AthleteTelegramId, IEnumerable<ExerciseDto> Exercises);
public record WorkoutResponseDto(string Name, ExerciseResponseDto[] Exercises);
public record ExerciseResponseDto(string Name, int Sets, string Reps);
