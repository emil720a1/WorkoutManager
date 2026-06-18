using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hangfire;
using WorkoutManager.Application;
using WorkoutManager.Infrastructure;
using WorkoutManager.Infrastructure.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

// Додавання шарів Application та Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// No Swagger needed
var app = builder.Build();

// Ввімкнення панелі Hangfire Dashboard
app.UseHangfireDashboard();

// Запуск планувальника повідомлень з Application Layer
using (var scope = app.Services.CreateScope())
{
    var scheduler = scope.ServiceProvider.GetRequiredService<WorkoutManager.Infrastructure.BackgroundJobs.WorkoutNotificationJob>();
    scheduler.ScheduleDailyNotificationsJob();
}

if (app.Environment.IsDevelopment())
{
    // Swagger нам тут не потрібен, бо немає HTTP-ендпоінтів
}

// Block 1: Minimal API Endpoints for BotGateway integration
app.MapPost("/api/v1/athletes/onboard", async (
    [Microsoft.AspNetCore.Mvc.FromBody] WorkoutManager.WorkoutAPI.OnboardRequest request,
    WorkoutManager.Application.Services.AthleteOnboardingService onboardingService,
    Microsoft.Extensions.Configuration.IConfiguration config,
    CancellationToken ct) =>
{
    var adminId = config.GetValue<long>("BotConfiguration:AdminTelegramId");
    var result = await onboardingService.TryBindAthleteAsync(adminId, request.AthleteTelegramId, request.Username, ct);
    
    return result.IsSuccess 
        ? Microsoft.AspNetCore.Http.Results.Ok() 
        : Microsoft.AspNetCore.Http.Results.BadRequest(result.Error);
});

app.MapGet("/api/v1/workouts/today", async (
    long telegramId,
    WorkoutManager.Application.Interfaces.IWorkoutService workoutService) =>
{
    var result = await workoutService.GetCurrentWorkoutAsync(telegramId);
    return result.IsSuccess 
        ? Microsoft.AspNetCore.Http.Results.Ok(result.Value) 
        : Microsoft.AspNetCore.Http.Results.BadRequest(new { Error = result.Error });
});

app.UseHttpsRedirection();
app.Run();

namespace WorkoutManager.WorkoutAPI
{
    public record OnboardRequest(long AthleteTelegramId, string Username);
}
