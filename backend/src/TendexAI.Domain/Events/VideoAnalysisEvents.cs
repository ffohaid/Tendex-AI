using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Events;

/// <summary>
/// Domain event raised when a video integrity analysis is requested.
/// </summary>
public sealed record VideoAnalysisRequestedEvent(
    Guid AnalysisId,
    Guid TenantId,
    Guid CompetitionId,
    string VideoFileReference) : IDomainEvent;

/// <summary>
/// Domain event raised when a video integrity analysis is completed.
/// </summary>
public sealed record VideoAnalysisCompletedEvent(
    Guid AnalysisId,
    Guid TenantId,
    Guid CompetitionId,
    VideoAnalysisStatus Status,
    TamperDetectionResult TamperResult,
    IdentityVerificationResult IdentityResult,
    decimal OverallConfidenceScore) : IDomainEvent;

/// <summary>
/// Domain event raised when a video analysis requires manual review.
/// </summary>
public sealed record VideoAnalysisManualReviewRequiredEvent(
    Guid AnalysisId,
    Guid TenantId,
    Guid CompetitionId,
    string Reason) : IDomainEvent;
