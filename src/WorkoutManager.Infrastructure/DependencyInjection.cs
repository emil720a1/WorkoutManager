using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkoutManager.Domain.Interfaces;
using WorkoutManager.Infrastructure.Persistence;
using WorkoutManager.Infrastructure.Persistence.Repositories;

namespace WorkoutManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAthleteRepository, AthleteRepository>();
        services.AddScoped<IProgramRepository, ProgramRepository>();

        return services;
    }
}
