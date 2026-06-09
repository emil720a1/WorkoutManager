using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.PostgreSql;
using StackExchange.Redis;
using WorkoutManager.Application.Interfaces;
using WorkoutManager.Domain.Interfaces;
using WorkoutManager.Infrastructure.BackgroundJobs;
using WorkoutManager.Infrastructure.Persistence;
using WorkoutManager.Infrastructure.Persistence.Repositories;
using WorkoutManager.Infrastructure.Services;
using WorkoutManager.Infrastructure.Workers;

namespace WorkoutManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IAthleteRepository, AthleteRepository>();
        services.AddScoped<IProgramRepository, ProgramRepository>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
        });
        services.AddSingleton<IStateService, RedisStateService>();

        services.AddTransient<MorningWorkoutJob>();

        services.AddHostedService<RedisSubscriberWorker>();

        services.AddHangfire(config => config
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))));

        services.AddHangfireServer();

        return services;
    }
}
