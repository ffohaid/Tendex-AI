using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Workflow.Services;

/// <summary>
/// Interface for the approval workflow orchestration service.
/// Manages the lifecycle of approval workflows for competition phase transitions.
/// </summary>
public interface IApprovalWorkflowService
{
    /// <summary>
    /// Initiates an approval workflow for a competition phase transition.
    /// Creates runtime approval steps from the active workflow definition
    /// or falls back to the hardcoded template.
    /// </summary>
    Task<Result<ApprovalWorkflowInitiationResult>> InitiateWorkflowAsync(
        Guid competitionId,
        Guid tenantId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        string initiatedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a specific step in the approval workflow.
    /// </summary>
    Task<Result<ApprovalActionResult>> ApproveStepAsync(
        Guid stepId,
        string userId,
        string? comment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a specific step in the approval workflow.
    /// Resets all subsequent steps back to pending.
    /// </summary>
    Task<Result<ApprovalActionResult>> RejectStepAsync(
        Guid stepId,
        string userId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of an approval workflow for a competition transition.
    /// </summary>
    Task<ApprovalWorkflowStatusResult> GetWorkflowStatusAsync(
        Guid competitionId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        CancellationToken cancellationToken = default);
}
