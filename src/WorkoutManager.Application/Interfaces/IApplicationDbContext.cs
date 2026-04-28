using Microsoft.EntityFrameworkCore;
using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Athlete> Athletes { get; }
    DbSet<Program> Programs { get; }
    DbSet<WorkoutDay> WorkoutDays { get; }
    DbSet<Exercise> Exercises { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
