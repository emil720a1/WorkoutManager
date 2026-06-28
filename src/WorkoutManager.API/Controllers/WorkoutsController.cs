using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WorkoutManager.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class WorkoutsController : ControllerBase
{
    // Draft for Epic: Workout Completion Tracking

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> MarkWorkoutCompleted(Guid id, CancellationToken ct)
    {
        // 1. Fetch Workout session/log from DB using id
        // var workout = await _dbContext.Workouts.FindAsync(id, ct);
        // if (workout == null) return NotFound();

        // 2. Set IsCompleted
        // workout.IsCompleted = true;
        // workout.CompletedAt = DateTime.UtcNow;

        // 3. Update Athlete Stats
        // var stats = await _dbContext.AthleteStats.FirstOrDefaultAsync(s => s.AthleteId == workout.AthleteId, ct);
        // if (stats != null) 
        // {
        //     stats.IncrementStreak();
        //     stats.AddTonnage(workout.CalculateTotalTonnage());
        // }

        // 4. Save changes
        // await _dbContext.SaveChangesAsync(ct);

        return Ok(new { Message = "Workout successfully marked as completed." });
    }
}
