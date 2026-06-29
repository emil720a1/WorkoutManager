using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WorkoutManager.Domain.Entities;
using WorkoutManager.Infrastructure.Persistence;
using Xunit;

namespace WorkoutManager.API.IntegrationTests;

public class WorkoutStatusTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly WebApplicationFactory<Program> _baseFactory;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public WorkoutStatusTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("workout_db_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        _factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        });
        
        _client = _factory.CreateClient();
        
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutManager.Infrastructure.Persistence.ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task MarkWorkoutCompleted_WhenValidId_ShouldReturnOkAndUpdateDatabase()
    {
        // Arrange
        var workoutId = Guid.NewGuid();
        var athleteId = Guid.NewGuid();
        
        using (var scope = _factory.Services.CreateScope())
        {
            // Assuming we use an InMemory DB or SQLite for the real factory in tests unless explicitly overridden
            // If the factory uses real PostgreSQL, this is where we'd seed it
            var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
            if (dbContext != null)
            {
                // Ensure db is created
                await dbContext.Database.EnsureCreatedAsync();

                var stats = new AthleteStats(Guid.NewGuid(), athleteId);
                var workout = new Workout(workoutId, athleteId, Guid.NewGuid());
                
                dbContext.AthleteStats.Add(stats);
                dbContext.Workouts.Add(workout);
                await dbContext.SaveChangesAsync();
                var savedWorkout = await dbContext.Workouts.FindAsync(workoutId);
                savedWorkout.Should().NotBeNull("Workout should be saved in Arrange step");
            }
        }

        // Act
        var response = await _client.PatchAsync($"/api/v1/workouts/{workoutId}/status", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
            if (dbContext != null)
            {
                var updatedWorkout = await dbContext.Workouts.FindAsync(workoutId);
                var updatedStats = await dbContext.AthleteStats.FirstOrDefaultAsync(s => s.AthleteId == athleteId);

                updatedWorkout.Should().NotBeNull();
                updatedWorkout!.IsCompleted.Should().BeTrue();
                updatedWorkout.CompletedAt.Should().NotBeNull();

                updatedStats.Should().NotBeNull();
                updatedStats!.CurrentStreak.Should().Be(1);
            }
        }
    }
}
