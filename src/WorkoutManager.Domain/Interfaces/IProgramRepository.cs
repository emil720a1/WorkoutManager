using WorkoutManager.Domain.Entities;

namespace WorkoutManager.Domain.Interfaces;

public interface IProgramRepository
{
    Task<Program?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Program>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Program program, CancellationToken cancellationToken = default);
    void Update(Program program);
    void Delete(Program program);
}