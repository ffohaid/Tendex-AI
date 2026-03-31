using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Features.Rfp.Services;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for CompetitionPermissionMatrix entity.
/// Implements the interface defined in CompetitionPermissionService.
/// </summary>
public sealed class CompetitionPermissionMatrixRepository : ICompetitionPermissionMatrixRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public CompetitionPermissionMatrixRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<CompetitionPermissionMatrix?> GetEntryAsync(
        CompetitionPhase phase,
        CommitteeRole committeeRole,
        SystemRole systemRole,
        string resourceType = "Competition",
        CancellationToken cancellationToken = default)
    {
        return await _context.CompetitionPermissionMatrices
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Phase == phase
                                      && m.CommitteeRole == committeeRole
                                      && m.SystemRole == systemRole
                                      && m.ResourceType == resourceType,
                cancellationToken);
    }

    public async Task<IReadOnlyList<CompetitionPermissionMatrix>> GetAllByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CompetitionPermissionMatrices
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .OrderBy(m => m.Phase)
            .ThenBy(m => m.CommitteeRole)
            .ThenBy(m => m.SystemRole)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CompetitionPermissionMatrix>> GetByPhaseAsync(
        CompetitionPhase phase,
        CancellationToken cancellationToken = default)
    {
        return await _context.CompetitionPermissionMatrices
            .AsNoTracking()
            .Where(m => m.Phase == phase)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<CompetitionPermissionMatrix> entries,
        CancellationToken cancellationToken = default)
    {
        await _context.CompetitionPermissionMatrices.AddRangeAsync(entries, cancellationToken);
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
