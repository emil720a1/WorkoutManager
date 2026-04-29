using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.PostgreSql;
using WorkoutManager.Application.Interfaces;
using WorkoutManager.Domain.Interfaces;
using WorkoutManager.Infrastructure.BackgroundJobs;
using WorkoutManager.Infrastructure.Persistence;
using WorkoutManager.Infrastructure.Persistence.Repositories;
using WorkoutManager.Infrastructure.Services;

namespace WorkoutManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<WorkoutManager.Application.Interfaces.IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IAthleteRepository, AthleteRepository>();
        services.AddScoped<IProgramRepository, ProgramRepository>();

        services.AddScoped<INotificationService, TelegramNotificationService>();
        services.AddTransient<MorningWorkoutJob>();

        services.AddHangfire(config => config
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))));

        services.AddHangfireServer();

        return services;
    }
}
