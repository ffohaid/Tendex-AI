using TendexAI.Domain.Common;

namespace TendexAI.Domain.Events;

/// <summary>
/// Raised when a financial evaluation session is started.
/// </summary>
public sealed record FinancialEvaluationStartedEvent(
    Guid EvaluationId, Guid CompetitionId, Guid TenantId) : IDomainEvent;

/// <summary>
/// Raised when financial envelopes are opened for technically-passed offers.
/// </summary>
public sealed record FinancialEnvelopesOpenedEvent(
    Guid CompetitionId, Guid TenantId, int OpenedCount) : IDomainEvent;

/// <summary>
/// Raised when all financial scores are submitted.
/// </summary>
public sealed record FinancialEvaluationCompletedEvent(
    Guid EvaluationId, Guid CompetitionId, Guid TenantId) : IDomainEvent;

/// <summary>
/// Raised when the financial evaluation report is approved.
/// </summary>
public sealed record FinancialReportApprovedEvent(
    Guid EvaluationId, Guid CompetitionId, Guid TenantId, string ApprovedBy) : IDomainEvent;

/// <summary>
/// Raised when an award recommendation is generated.
/// </summary>
public sealed record AwardRecommendationGeneratedEvent(
    Guid RecommendationId, Guid CompetitionId, Guid TenantId,
    Guid RecommendedOfferId, string RecommendedSupplierName) : IDomainEvent;

/// <summary>
/// Raised when an award recommendation is approved by the authority holder.
/// </summary>
public sealed record AwardApprovedEvent(
    Guid RecommendationId, Guid CompetitionId, Guid TenantId,
    string ApprovedBy) : IDomainEvent;

/// <summary>
/// Raised when evaluation minutes are generated.
/// </summary>
public sealed record EvaluationMinutesGeneratedEvent(
    Guid MinutesId, Guid CompetitionId, Guid TenantId,
    string MinutesType) : IDomainEvent;
