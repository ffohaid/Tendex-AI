using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository interface for PhaseTransitionHistory entity.
/// </summary>
public interface IPhaseTransitionHistoryRepository
{
    Task<IReadOnlyList<PhaseTransitionHistory>> GetByCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PhaseTransitionHistory history,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository implementation for PhaseTransitionHistory entity.
/// Immutable audit trail — no update or delete operations.
/// </summary>
public sealed class PhaseTransitionHistoryRepository : IPhaseTransitionHistoryRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public PhaseTransitionHistoryRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<IReadOnlyList<PhaseTransitionHistory>> GetByCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PhaseTransitionHistories
            .AsNoTracking()
            .Where(h => h.CompetitionId == competitionId)
            .OrderBy(h => h.TransitionedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        PhaseTransitionHistory history,
        CancellationToken cancellationToken = default)
    {
        await _context.PhaseTransitionHistories.AddAsync(history, cancellationToken);
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
