namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Repository interface for VideoIntegrityAnalysis aggregate.
/// Follows the repository pattern defined in the Domain layer.
/// </summary>
public interface IVideoIntegrityAnalysisRepository
{
    /// <summary>
    /// Gets a video integrity analysis by its unique identifier, including flags.
    /// </summary>
    Task<VideoIntegrityAnalysis?> GetByIdWithFlagsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all video integrity analyses for a specific competition.
    /// </summary>
    Task<IReadOnlyList<VideoIntegrityAnalysis>> GetByCompetitionIdAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all video integrity analyses for a specific supplier offer.
    /// </summary>
    Task<IReadOnlyList<VideoIntegrityAnalysis>> GetBySupplierOfferIdAsync(
        Guid supplierOfferId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending analyses that need to be processed.
    /// </summary>
    Task<IReadOnlyList<VideoIntegrityAnalysis>> GetPendingAnalysesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest analysis for a specific video file reference.
    /// </summary>
    Task<VideoIntegrityAnalysis?> GetLatestByVideoReferenceAsync(
        string videoFileReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new video integrity analysis record.
    /// </summary>
    Task AddAsync(
        VideoIntegrityAnalysis analysis,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing video integrity analysis record.
    /// </summary>
    Task UpdateAsync(
        VideoIntegrityAnalysis analysis,
        CancellationToken cancellationToken = default);
}
