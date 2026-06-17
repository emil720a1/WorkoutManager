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

builder.Services.AddOpenApi();

var app = builder.Build();

// Ввімкнення панелі Hangfire Dashboard
app.UseHangfireDashboard();

// Реєстрація щоденної ранкової задачі надсиланняreminder-ів
RecurringJob.AddOrUpdate<MorningWorkoutJob>(
    "MorningWorkoutReminder",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 7 * * *");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.Run();
