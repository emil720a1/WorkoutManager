using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public DbSet<Athlete> Athletes => Set<Athlete>();
    public DbSet<Program> Programs => Set<Program>();
    public DbSet<WorkoutDay> WorkoutDays => Set<WorkoutDay>();
    public DbSet<Exercise> Exercises => Set<Exercise>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
