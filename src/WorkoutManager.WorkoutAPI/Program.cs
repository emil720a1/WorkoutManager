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

// Запуск планувальника повідомлень з Application Layer
using (var scope = app.Services.CreateScope())
{
    var scheduler = scope.ServiceProvider.GetRequiredService<WorkoutManager.Infrastructure.BackgroundJobs.WorkoutNotificationJob>();
    scheduler.ScheduleDailyNotificationsJob();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.Run();
