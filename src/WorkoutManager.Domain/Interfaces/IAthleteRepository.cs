using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Domain.Interfaces;

public interface IAthleteRepository
{
    Task<Athlete?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Athlete?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyCollection<Athlete>> GetAthletesToNotifyAsync(int dayOfWeek, CancellationToken cancellationToken = default);
    Task AddAsync(Athlete athlete, CancellationToken cancellationToken = default);
    void Update(Athlete athlete);
}