namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Repository interface for AiOfferAnalysis entities.
/// </summary>
public interface IAiOfferAnalysisRepository
{
    /// <summary>
    /// Gets an AI offer analysis by its unique identifier, including criterion analyses.
    /// </summary>
    Task<AiOfferAnalysis?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an AI offer analysis for a specific supplier offer within an evaluation.
    /// </summary>
    Task<AiOfferAnalysis?> GetByOfferIdAsync(
        Guid technicalEvaluationId,
        Guid supplierOfferId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all AI offer analyses for a technical evaluation.
    /// </summary>
    Task<IReadOnlyList<AiOfferAnalysis>> GetByEvaluationIdAsync(
        Guid technicalEvaluationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all AI offer analyses for a technical evaluation with criterion details.
    /// </summary>
    Task<IReadOnlyList<AiOfferAnalysis>> GetByEvaluationIdWithDetailsAsync(
        Guid technicalEvaluationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an analysis already exists for a specific offer in an evaluation.
    /// </summary>
    Task<bool> ExistsForOfferAsync(
        Guid technicalEvaluationId,
        Guid supplierOfferId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new AI offer analysis.
    /// </summary>
    Task AddAsync(AiOfferAnalysis analysis, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing AI offer analysis.
    /// </summary>
    void Update(AiOfferAnalysis analysis);

    /// <summary>
    /// Persists all pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
