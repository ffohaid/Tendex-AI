using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for SupplierOffer entities.
/// Operates against the tenant-specific database using ITenantDbContextFactory.
///
/// Performance optimizations (TASK-703):
/// - AsNoTracking() on all read-only queries to reduce change tracker overhead.
/// </summary>
public sealed class SupplierOfferRepository : ISupplierOfferRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public SupplierOfferRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<SupplierOffer?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SupplierOffers
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierOffer>> GetByCompetitionIdAsync(
        Guid competitionId, CancellationToken cancellationToken = default)
    {
        return await _context.SupplierOffers
            .Where(o => o.CompetitionId == competitionId && !o.IsDeleted)
            .OrderBy(o => o.BlindCode)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierOffer>> GetPassedOffersAsync(
        Guid competitionId, CancellationToken cancellationToken = default)
    {
        return await _context.SupplierOffers
            .Where(o => o.CompetitionId == competitionId &&
                        o.TechnicalResult == OfferTechnicalResult.Passed)
            .OrderByDescending(o => o.TechnicalTotalScore)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetOfferCountAsync(
        Guid competitionId, CancellationToken cancellationToken = default)
    {
        return await _context.SupplierOffers
            .CountAsync(o => o.CompetitionId == competitionId && !o.IsDeleted, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid competitionId,
        string supplierIdentifier,
        CancellationToken cancellationToken = default)
    {
        return await _context.SupplierOffers
            .AnyAsync(o => o.CompetitionId == competitionId &&
                           o.SupplierIdentifier == supplierIdentifier &&
                           !o.IsDeleted,
                cancellationToken);
    }

    public async Task AddAsync(
        SupplierOffer offer, CancellationToken cancellationToken = default)
    {
        await _context.SupplierOffers.AddAsync(offer, cancellationToken);
    }

    public void Update(SupplierOffer offer)
    {
        _context.SupplierOffers.Update(offer);
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
