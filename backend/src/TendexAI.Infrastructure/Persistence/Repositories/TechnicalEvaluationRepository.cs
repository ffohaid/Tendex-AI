using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for TechnicalEvaluation aggregate root.
/// Operates against the tenant-specific database using ITenantDbContextFactory.
///
/// Performance optimizations (TASK-703):
/// - AsNoTracking() on read-only queries to reduce change tracker overhead.
/// - AsSplitQuery() retained on multi-Include queries.
/// </summary>
public sealed class TechnicalEvaluationRepository : ITechnicalEvaluationRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public TechnicalEvaluationRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<TechnicalEvaluation?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TechnicalEvaluations
            .AsNoTracking()
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
