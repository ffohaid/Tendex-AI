using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for EvaluationMinutes aggregate root.
/// Operates against the tenant-specific database using ITenantDbContextFactory.
/// </summary>
public sealed class EvaluationMinutesRepository : IEvaluationMinutesRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public EvaluationMinutesRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<EvaluationMinutes?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EvaluationMinutes
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<EvaluationMinutes>> GetByCompetitionIdAsync(
        Guid competitionId, CancellationToken cancellationToken = default)
    {
        return await _context.EvaluationMinutes
            .AsNoTracking()
            .Where(e => e.CompetitionId == competitionId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<EvaluationMinutes?> GetWithSignatoriesAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EvaluationMinutes
            .Include(e => e.Signatories)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        EvaluationMinutes minutes, CancellationToken cancellationToken = default)
    {
        await _context.EvaluationMinutes.AddAsync(minutes, cancellationToken);
    }

    public void Update(EvaluationMinutes minutes)
    {
        _context.EvaluationMinutes.Update(minutes);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}
