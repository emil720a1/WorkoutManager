using Microsoft.EntityFrameworkCore;
using WorkoutManager.Domain.Entities;
using WorkoutManager.Domain.Interfaces;

namespace WorkoutManager.Infrastructure.Persistence.Repositories;

public class ProgramRepository : IProgramRepository
{
    private readonly ApplicationDbContext _context;

    public ProgramRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Program?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Programs
            .Include(p => p.Days)
                .ThenInclude(d => d.Exercises)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Program>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Programs.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Program program, CancellationToken cancellationToken = default)
    {
        await _context.Programs.AddAsync(program, cancellationToken);
    }

    public void Update(Program program)
    {
        _context.Programs.Update(program);
    }

    public void Delete(Program program)
    {
        _context.Programs.Remove(program);
    }
}
