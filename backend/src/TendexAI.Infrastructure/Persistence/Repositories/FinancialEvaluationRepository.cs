using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for FinancialEvaluation aggregate root.
/// Operates against the tenant-specific database using ITenantDbContextFactory.
/// </summary>
public sealed class FinancialEvaluationRepository : IFinancialEvaluationRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public FinancialEvaluationRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<FinancialEvaluation?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialEvaluations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<FinancialEvaluation?> GetByCompetitionIdAsync(
        Guid competitionId, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialEvaluations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CompetitionId == competitionId, cancellationToken);
    }

    /// <summary>
    /// Gets a tracked FinancialEvaluation by competition ID for modification.
    /// Do NOT use AsNoTracking here — callers modify and save the entity.
    /// </summary>
    public async Task<FinancialEvaluation?> GetByCompetitionIdForUpdateAsync(
        Guid competitionId, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialEvaluations
            .FirstOrDefaultAsync(e => e.CompetitionId == competitionId, cancellationToken);
    }

    public async Task<FinancialEvaluation?> GetWithItemsAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialEvaluations
            .Include(e => e.OfferItems)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<FinancialEvaluation?> GetWithScoresAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialEvaluations
            .Include(e => e.Scores)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<FinancialEvaluation?> GetFullAsync(
        Guid competitionId, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialEvaluations
            .Include(e => e.Scores)
            .Include(e => e.OfferItems)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.CompetitionId == competitionId, cancellationToken);
    }

    public async Task AddAsync(
        FinancialEvaluation evaluation, CancellationToken cancellationToken = default)
    {
        await _context.FinancialEvaluations.AddAsync(evaluation, cancellationToken);
    }

    public void Update(FinancialEvaluation evaluation)
    {
        _context.FinancialEvaluations.Update(evaluation);
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
