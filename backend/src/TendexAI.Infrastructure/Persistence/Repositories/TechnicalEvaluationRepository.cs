using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for TechnicalEvaluation aggregate root.
/// Operates against the tenant-specific database using ITenantDbContextFactory.
/// </summary>
public sealed class TechnicalEvaluationRepository : ITechnicalEvaluationRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private readonly ILogger<TechnicalEvaluationRepository> _logger;
    private bool _disposed;

    public TechnicalEvaluationRepository(
        ITenantDbContextFactory tenantDbContextFactory,
        ILogger<TechnicalEvaluationRepository> logger)
    {
        _context = tenantDbContextFactory.CreateDbContext();
        _logger = logger;
    }

    public async Task<TechnicalEvaluation?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TechnicalEvaluations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets a tracked entity by ID for modification (no AsNoTracking).
    /// </summary>
    public async Task<TechnicalEvaluation?> GetByIdForUpdateAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TechnicalEvaluations
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<TechnicalEvaluation?> GetByIdWithScoresAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        // NOTE: Do NOT use AsNoTracking here — this query is used by
        // SubmitTechnicalScoreCommandHandler which modifies the entity.
        return await _context.TechnicalEvaluations
            .Include(e => e.Scores)
            .Include(e => e.AiScores)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<TechnicalEvaluation?> GetByCompetitionIdAsync(
        Guid competitionId, CancellationToken cancellationToken = default)
    {
        return await _context.TechnicalEvaluations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CompetitionId == competitionId, cancellationToken);
    }

    public async Task<TechnicalEvaluation?> GetByCompetitionIdWithScoresAsync(
        Guid competitionId, CancellationToken cancellationToken = default)
    {
        // NOTE: Do NOT use AsNoTracking here — this query is used for
        // modification operations (score submission, completion, etc.).
        return await _context.TechnicalEvaluations
            .Include(e => e.Scores)
            .Include(e => e.AiScores)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.CompetitionId == competitionId, cancellationToken);
    }

    public async Task<bool> ExistsForCompetitionAsync(
        Guid competitionId, CancellationToken cancellationToken = default)
    {
        return await _context.TechnicalEvaluations
            .AnyAsync(e => e.CompetitionId == competitionId, cancellationToken);
    }

    public async Task AddAsync(
        TechnicalEvaluation evaluation, CancellationToken cancellationToken = default)
    {
        await _context.TechnicalEvaluations.AddAsync(evaluation, cancellationToken);
    }

    public void Update(TechnicalEvaluation evaluation)
    {
        _context.TechnicalEvaluations.Update(evaluation);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Debug: log all tracked entity states before saving
        foreach (var entry in _context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Unchanged)
            {
                _logger.LogWarning(
                    "EF ChangeTracker: Entity={EntityType}, State={State}, PK={PrimaryKey}",
                    entry.Entity.GetType().Name,
                    entry.State,
                    entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue);
            }
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                _logger.LogError(
                    "Concurrency conflict: Entity={EntityType}, State={State}",
                    entry.Entity.GetType().Name,
                    entry.State);

                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues == null)
                {
                    _logger.LogError("Entity does NOT exist in database (was expected to exist for update)");
                }
                else
                {
                    _logger.LogError("Entity EXISTS in database. Current DB values available.");
                }
            }
            throw;
        }
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
