using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Events;

/// <summary>
/// Raised when a technical evaluation session is started.
/// </summary>
public sealed record TechnicalEvaluationStartedEvent(
    Guid EvaluationId,
    Guid CompetitionId,
    Guid TenantId) : IDomainEvent;

/// <summary>
/// Raised when a committee member submits a score.
/// </summary>
public sealed record TechnicalScoreSubmittedEvent(
    Guid EvaluationId,
    Guid ScoreId,
    Guid SupplierOfferId,
    Guid CriterionId,
    string EvaluatorUserId) : IDomainEvent;

/// <summary>
/// Raised when all technical scores are submitted.
/// </summary>
public sealed record TechnicalEvaluationCompletedEvent(
    Guid EvaluationId,
    Guid CompetitionId,
    Guid TenantId) : IDomainEvent;

/// <summary>
/// Raised when the technical evaluation report is approved by the committee chair.
/// This event triggers the unlocking of financial envelopes for passed offers.
/// </summary>
public sealed record TechnicalReportApprovedEvent(
    Guid EvaluationId,
    Guid CompetitionId,
    Guid TenantId,
    string ApprovedBy,
    int PassedOfferCount,
    int FailedOfferCount) : IDomainEvent;

/// <summary>
/// Raised when the technical evaluation report is rejected.
/// </summary>
public sealed record TechnicalReportRejectedEvent(
    Guid EvaluationId,
    Guid CompetitionId,
    Guid TenantId,
    string RejectedBy,
    string Reason) : IDomainEvent;
