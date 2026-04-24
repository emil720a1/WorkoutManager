using Microsoft.Extensions.DependencyInjection;
using WorkoutManager.Application.Interfaces;
using WorkoutManager.Application.Services;

namespace WorkoutManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IWorkoutService, WorkoutService>();
        return services;
    }
}
