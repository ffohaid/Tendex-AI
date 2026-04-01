using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<FinancialEvaluationRepository> _logger;
    private bool _disposed;

    public FinancialEvaluationRepository(
        ITenantDbContextFactory tenantDbContextFactory,
        ILogger<FinancialEvaluationRepository> logger)
    {
        _context = tenantDbContextFactory.CreateDbContext();
        _logger = logger;
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
        // Fix entity states: entities added via navigation property collections
        // may be incorrectly tracked as Modified instead of Added.
        foreach (var entry in _context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is FinancialScore)
                {
                    var pkProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                    if (pkProp != null)
                    {
                        var scoreId = (Guid)pkProp.CurrentValue!;
                        var existsInDb = await _context.Set<FinancialScore>()
                            .AsNoTracking()
                            .AnyAsync(s => s.Id == scoreId);

                        if (!existsInDb)
                        {
                            _logger.LogWarning(
                                "Correcting entity state from Modified to Added: {EntityType}, PK={PK}",
                                entry.Entity.GetType().Name, scoreId);
                            entry.State = EntityState.Added;
                        }
                    }
                }
                else if (entry.Entity is FinancialOfferItem)
                {
                    var pkProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                    if (pkProp != null)
                    {
                        var itemId = (Guid)pkProp.CurrentValue!;
                        var existsInDb = await _context.Set<FinancialOfferItem>()
                            .AsNoTracking()
                            .AnyAsync(i => i.Id == itemId);

                        if (!existsInDb)
                        {
                            _logger.LogWarning(
                                "Correcting entity state from Modified to Added: {EntityType}, PK={PK}",
                                entry.Entity.GetType().Name, itemId);
                            entry.State = EntityState.Added;
                        }
                    }
                }
            }
        }

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
