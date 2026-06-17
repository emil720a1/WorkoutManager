using Microsoft.EntityFrameworkCore;
using WorkoutManager.Domain.Entities;
using WorkoutManager.Domain.Interfaces;

namespace WorkoutManager.Infrastructure.Persistence.Repositories;

public class AthleteRepository : IAthleteRepository
{
    private readonly ApplicationDbContext _context;

    public AthleteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Athlete?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Athletes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Athlete?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return await _context.Athletes.FirstOrDefaultAsync(a => a.TelegramId == telegramId, cancellationToken);
    }

    // Lazy Binding: find unbound (pending) athlete by username
    public async Task<Athlete?> GetPendingByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.TrimStart('@').ToLowerInvariant();
        return await _context.Athletes
            .FirstOrDefaultAsync(
                a => a.Username == normalized && a.Status == AthleteStatus.Pending,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Athlete>> GetAthletesToNotifyAsync(int dayOfWeek, CancellationToken cancellationToken = default)
    {
        return await _context.Athletes
            .Where(a => a.Status == AthleteStatus.Active)
            // .Where(a => a.NotificationDays.Contains(dayOfWeek))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Athlete athlete, CancellationToken cancellationToken = default)
    {
        await _context.Athletes.AddAsync(athlete, cancellationToken);
    }

    public void Update(Athlete athlete)
    {
        _context.Athletes.Update(athlete);
    }
}
