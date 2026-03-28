using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.VideoAnalysis.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.VideoAnalysis.Commands.RecordManualReview;

/// <summary>
/// Command to record a manual review decision on a video integrity analysis.
/// Used when an analysis result is inconclusive and requires human judgment.
/// </summary>
public sealed record RecordManualReviewCommand(
    Guid AnalysisId,
    string ReviewerUserId,
    VideoAnalysisStatus OverrideStatus,
    string? Notes) : ICommand<VideoIntegrityAnalysisDto>;
