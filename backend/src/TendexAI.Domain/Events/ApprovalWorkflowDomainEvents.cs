using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Events;

/// <summary>
/// Raised when approval workflow steps are created for a competition transition.
/// </summary>
public sealed record ApprovalWorkflowInitiatedEvent(
    Guid CompetitionId,
    Guid TenantId,
    CompetitionStatus FromStatus,
    CompetitionStatus ToStatus,
    int TotalSteps,
    string InitiatedByUserId) : IDomainEvent;

/// <summary>
/// Raised when an approval step is approved.
/// </summary>
public sealed record ApprovalStepApprovedEvent(
    Guid ApprovalStepId,
    Guid CompetitionId,
    Guid TenantId,
    int StepOrder,
    string StepNameAr,
    string StepNameEn,
    string ApprovedByUserId,
    string? Comment) : IDomainEvent;

/// <summary>
/// Raised when an approval step is rejected.
/// </summary>
public sealed record ApprovalStepRejectedEvent(
    Guid ApprovalStepId,
    Guid CompetitionId,
    Guid TenantId,
    int StepOrder,
    string StepNameAr,
    string StepNameEn,
    string RejectedByUserId,
    string Reason) : IDomainEvent;

/// <summary>
/// Raised when all approval steps for a transition are completed.
/// </summary>
public sealed record ApprovalWorkflowCompletedEvent(
    Guid CompetitionId,
    Guid TenantId,
    CompetitionStatus FromStatus,
    CompetitionStatus ToStatus) : IDomainEvent;
