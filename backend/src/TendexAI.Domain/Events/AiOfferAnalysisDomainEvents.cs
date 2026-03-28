using TendexAI.Domain.Common;

namespace TendexAI.Domain.Events;

/// <summary>
/// Raised when an AI offer analysis is completed for a supplier offer.
/// </summary>
public sealed record AiOfferAnalysisCompletedEvent(
    Guid AnalysisId,
    Guid EvaluationId,
    Guid SupplierOfferId,
    Guid CompetitionId,
    Guid TenantId,
    string BlindCode,
    decimal OverallComplianceScore) : IDomainEvent;

/// <summary>
/// Raised when an AI offer analysis fails during processing.
/// </summary>
public sealed record AiOfferAnalysisFailedEvent(
    Guid EvaluationId,
    Guid SupplierOfferId,
    Guid CompetitionId,
    Guid TenantId,
    string BlindCode,
    string ErrorMessage) : IDomainEvent;

/// <summary>
/// Raised when a human committee member reviews an AI offer analysis.
/// </summary>
public sealed record AiOfferAnalysisReviewedEvent(
    Guid AnalysisId,
    Guid EvaluationId,
    Guid CompetitionId,
    Guid TenantId,
    string ReviewedBy) : IDomainEvent;

/// <summary>
/// Raised when all offers in a technical evaluation have been analyzed by AI.
/// </summary>
public sealed record AllOffersAiAnalysisCompletedEvent(
    Guid EvaluationId,
    Guid CompetitionId,
    Guid TenantId,
    int TotalOffers,
    int SuccessfulAnalyses,
    int FailedAnalyses) : IDomainEvent;
