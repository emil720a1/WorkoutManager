namespace WorkoutManager.Infrastructure.BackgroundJobs;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutManager.Application.Interfaces;

public class MorningWorkoutJob(IApplicationDbContext dbContext, INotificationService notificationService)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var currentDay = DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)DateTime.UtcNow.DayOfWeek;

        var programIds = await dbContext.Programs
            .AsNoTracking()
            .Where(p => p.Days.Any(d => d.DayNumber == currentDay))
            .Select(p => p.Id)
            .ToListAsync(ct);

        var telegramIds = await dbContext.Athletes
            .AsNoTracking()
            .Where(a => a.CurrentProgramId.HasValue && programIds.Contains(a.CurrentProgramId.Value))
            .Select(a => a.TelegramId)
            .ToListAsync(ct);

        foreach (var id in telegramIds)
        {
            await notificationService.SendWorkoutReminderAsync(id, "Time for your morning workout.", ct);
        }
    }
}
