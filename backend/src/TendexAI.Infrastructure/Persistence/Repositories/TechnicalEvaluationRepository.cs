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

    /// <summary>
    /// Explicitly adds a TechnicalScore entity to the DbContext as Added.
    /// This avoids the issue where adding a score via the navigation property
    /// collection (_scores.Add) causes EF Core to mark it as Modified instead of Added.
    /// </summary>
    public async Task AddScoreAsync(TechnicalScore score, CancellationToken cancellationToken = default)
    {
        await _context.Set<TechnicalScore>().AddAsync(score, cancellationToken);
    }

    public void Update(TechnicalEvaluation evaluation)
    {
        _context.TechnicalEvaluations.Update(evaluation);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Fix entity states: entities added via navigation property collections
        // may be incorrectly tracked as Modified instead of Added.
        // Detect and correct this before saving.
        foreach (var entry in _context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                // Check if this entity actually exists in the database
                // by comparing original values - if all original values are default,
                // this is a new entity that should be Added
                var pkProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                if (pkProp != null)
                {
                    var pkValue = pkProp.CurrentValue;
                    _logger.LogInformation(
                        "Checking Modified entity: {EntityType}, PK={PK}",
                        entry.Entity.GetType().Name, pkValue);

                    // For entities that were added via navigation property but marked as Modified,
                    // check if they exist in the database
                    if (entry.Entity is TechnicalScore)
                    {
                        var scoreId = (Guid)pkValue!;
                        var existsInDb = await _context.Set<TechnicalScore>()
                            .AsNoTracking()
                            .AnyAsync(s => s.Id == scoreId, cancellationToken);

                        if (!existsInDb)
                        {
                            _logger.LogWarning(
                                "Correcting entity state from Modified to Added: {EntityType}, PK={PK}",
                                entry.Entity.GetType().Name, pkValue);
                            entry.State = EntityState.Added;
                        }
                    }
                    else if (entry.Entity is AiTechnicalScore)
                    {
                        var scoreId = (Guid)pkValue!;
                        var existsInDb = await _context.Set<AiTechnicalScore>()
                            .AsNoTracking()
                            .AnyAsync(s => s.Id == scoreId, cancellationToken);

                        if (!existsInDb)
                        {
                            _logger.LogWarning(
                                "Correcting entity state from Modified to Added: {EntityType}, PK={PK}",
                                entry.Entity.GetType().Name, pkValue);
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
