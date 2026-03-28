using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for AiOfferAnalysis entities.
/// Operates against the tenant-specific database using ITenantDbContextFactory.
/// </summary>
public sealed class AiOfferAnalysisRepository : IAiOfferAnalysisRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public AiOfferAnalysisRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    /// <inheritdoc />
    public async Task<AiOfferAnalysis?> GetByIdWithDetailsAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AiOfferAnalysis>()
            .Include(a => a.CriterionAnalyses)
            .Include(a => a.SupplierOffer)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiOfferAnalysis?> GetByOfferIdAsync(
        Guid technicalEvaluationId,
        Guid supplierOfferId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<AiOfferAnalysis>()
            .Include(a => a.CriterionAnalyses)
            .FirstOrDefaultAsync(a =>
                a.TechnicalEvaluationId == technicalEvaluationId &&
                a.SupplierOfferId == supplierOfferId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiOfferAnalysis>> GetByEvaluationIdAsync(
        Guid technicalEvaluationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<AiOfferAnalysis>()
            .Where(a => a.TechnicalEvaluationId == technicalEvaluationId)
            .OrderBy(a => a.BlindCode)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiOfferAnalysis>> GetByEvaluationIdWithDetailsAsync(
        Guid technicalEvaluationId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<AiOfferAnalysis>()
            .Include(a => a.CriterionAnalyses)
            .Include(a => a.SupplierOffer)
            .AsSplitQuery()
            .Where(a => a.TechnicalEvaluationId == technicalEvaluationId)
            .OrderBy(a => a.BlindCode)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForOfferAsync(
        Guid technicalEvaluationId,
        Guid supplierOfferId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<AiOfferAnalysis>()
            .AnyAsync(a =>
                a.TechnicalEvaluationId == technicalEvaluationId &&
                a.SupplierOfferId == supplierOfferId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(
        AiOfferAnalysis analysis,
        CancellationToken cancellationToken = default)
    {
        await _context.Set<AiOfferAnalysis>().AddAsync(analysis, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(AiOfferAnalysis analysis)
    {
        _context.Set<AiOfferAnalysis>().Update(analysis);
    }

    /// <inheritdoc />
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
