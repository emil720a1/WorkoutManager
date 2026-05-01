using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Athlete> Athletes { get; }
    DbSet<Program> Programs { get; }
    DbSet<WorkoutDay> WorkoutDays { get; }
    DbSet<Exercise> Exercises { get; }
    
    DatabaseFacade Database { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
