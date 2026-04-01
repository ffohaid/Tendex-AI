namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Repository interface for TechnicalEvaluation aggregate.
/// </summary>
public interface ITechnicalEvaluationRepository
{
    Task<TechnicalEvaluation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a tracked entity by ID for modification (no AsNoTracking).</summary>
    Task<TechnicalEvaluation?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TechnicalEvaluation?> GetByIdWithScoresAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TechnicalEvaluation?> GetByCompetitionIdAsync(Guid competitionId, CancellationToken cancellationToken = default);

    Task<TechnicalEvaluation?> GetByCompetitionIdWithScoresAsync(Guid competitionId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForCompetitionAsync(Guid competitionId, CancellationToken cancellationToken = default);

    Task AddAsync(TechnicalEvaluation evaluation, CancellationToken cancellationToken = default);

    void Update(TechnicalEvaluation evaluation);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
