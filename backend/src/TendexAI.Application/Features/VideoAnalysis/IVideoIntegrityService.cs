using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.VideoAnalysis;

/// <summary>
/// Service interface for performing video integrity analysis.
/// Implementations handle the actual AI-driven analysis pipeline including
/// tamper detection and identity verification.
/// Defined in Application layer; implemented in Infrastructure layer.
/// </summary>
public interface IVideoIntegrityService
{
    /// <summary>
    /// Performs a complete video integrity analysis including:
    /// 1. Scene authenticity verification
    /// 2. Tamper/re-recording detection
    /// 3. Identity verification of the filmed person
    /// </summary>
    /// <param name="analysis">The analysis entity to process and update.</param>
    /// <param name="referenceImageUrl">Optional reference image URL for identity verification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated analysis entity with results.</returns>
    Task<VideoIntegrityAnalysis> AnalyzeVideoAsync(
        VideoIntegrityAnalysis analysis,
        string? referenceImageUrl = null,
        CancellationToken cancellationToken = default);
}
