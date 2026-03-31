using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Entities.Workflow;
using TendexAI.Domain.Enums;
using TendexAI.Domain.StateMachine;

namespace TendexAI.Application.Features.Workflow.Services;

/// <summary>
/// Core orchestration service for the approval workflow engine.
/// Replaces the static <c>ApprovalWorkflowTemplate</c> with a dynamic,
/// database-driven workflow that supports sequential, parallel, and conditional steps.
///
/// This service is responsible for:
/// 1. Initiating approval workflows by creating runtime steps from definitions.
/// 2. Processing approval/rejection actions on individual steps.
/// 3. Advancing the workflow when all steps at a given order are completed.
/// 4. Triggering competition state transitions when all steps are completed.
/// </summary>
public sealed class ApprovalWorkflowService : IApprovalWorkflowService
{
    private readonly IWorkflowDefinitionRepository _workflowDefinitionRepository;
    private readonly IApprovalWorkflowStepRepository _approvalStepRepository;
    private readonly ICompetitionRepository _competitionRepository;

    public ApprovalWorkflowService(
        IWorkflowDefinitionRepository workflowDefinitionRepository,
        IApprovalWorkflowStepRepository approvalStepRepository,
        ICompetitionRepository competitionRepository)
    {
        _workflowDefinitionRepository = workflowDefinitionRepository;
        _approvalStepRepository = approvalStepRepository;
        _competitionRepository = competitionRepository;
    }

    /// <inheritdoc />
    public async Task<Result<ApprovalWorkflowInitiationResult>> InitiateWorkflowAsync(
        Guid competitionId,
        Guid tenantId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        string initiatedByUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Get the active workflow definition for this transition
        var definition = await _workflowDefinitionRepository.GetActiveByTransitionAsync(
            tenantId, fromStatus, toStatus, cancellationToken);

        if (definition is null)
        {
            // Fall back to the hardcoded template for backward compatibility
            return await InitiateFromHardcodedTemplateAsync(
                competitionId, tenantId, fromStatus, toStatus, initiatedByUserId, cancellationToken);
        }

        // 2. Check if there are already steps for this transition (prevent duplicates)
        var existingSteps = await _approvalStepRepository.GetByCompetitionTransitionAsync(
            competitionId, fromStatus, toStatus, cancellationToken);

        if (existingSteps.Count > 0)
            return Result.Failure<ApprovalWorkflowInitiationResult>(
                "يوجد مسار اعتماد نشط بالفعل لهذه المنافسة والمرحلة.");

        // 3. Create runtime approval steps from the definition
        var steps = new List<ApprovalWorkflowStep>();
        foreach (var stepDef in definition.Steps.Where(s => s.IsActive))
        {
            // TODO: Evaluate conditional steps using ConditionExpression
            if (stepDef.IsConditional && !string.IsNullOrEmpty(stepDef.ConditionExpression))
            {
                // For now, include all conditional steps; condition evaluation will be added later
            }

            var step = ApprovalWorkflowStep.Create(
                competitionId: competitionId,
                tenantId: tenantId,
                fromStatus: fromStatus,
                toStatus: toStatus,
                stepOrder: stepDef.StepOrder,
                requiredRole: stepDef.RequiredSystemRole,
                requiredCommitteeRole: stepDef.RequiredCommitteeRole,
                stepNameAr: stepDef.StepNameAr,
                stepNameEn: stepDef.StepNameEn,
                createdBy: initiatedByUserId,
                workflowStepDefinitionId: stepDef.Id,
                slaHours: stepDef.SlaHours);

            steps.Add(step);
        }

        if (steps.Count == 0)
            return Result.Failure<ApprovalWorkflowInitiationResult>(
                "لا توجد خطوات اعتماد مفعّلة في مسار الاعتماد المحدد.");

        await _approvalStepRepository.AddRangeAsync(steps, cancellationToken);
        await _approvalStepRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(new ApprovalWorkflowInitiationResult(
            WorkflowDefinitionId: definition.Id,
            TotalSteps: steps.Count,
            StepIds: steps.Select(s => s.Id).ToList()));
    }

    /// <inheritdoc />
    public async Task<Result<ApprovalActionResult>> ApproveStepAsync(
        Guid stepId,
        string userId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var step = await _approvalStepRepository.GetByIdForUpdateAsync(stepId, cancellationToken);
        if (step is null)
            return Result.Failure<ApprovalActionResult>("خطوة الاعتماد غير موجودة.");

        // Verify this step is currently actionable
        var currentPendingSteps = await _approvalStepRepository.GetCurrentPendingStepsAsync(
            step.CompetitionId, step.FromStatus, step.ToStatus, cancellationToken);

        if (!currentPendingSteps.Any(s => s.Id == stepId))
            return Result.Failure<ApprovalActionResult>(
                "هذه الخطوة ليست الخطوة الحالية في مسار الاعتماد.");

        // Approve the step
        var result = step.Approve(userId, comment);
        if (result.IsFailure)
            return Result.Failure<ApprovalActionResult>(result.Error!);

        _approvalStepRepository.Update(step);
        await _approvalStepRepository.SaveChangesAsync(cancellationToken);

        // Check if all steps at this order are now completed
        var allStepsCompleted = await _approvalStepRepository.AreAllStepsCompletedAsync(
            step.CompetitionId, step.FromStatus, step.ToStatus, cancellationToken);

        return Result.Success(new ApprovalActionResult(
            StepId: stepId,
            CompetitionId: step.CompetitionId,
            IsWorkflowCompleted: allStepsCompleted,
            FromStatus: step.FromStatus,
            ToStatus: step.ToStatus));
    }

    /// <inheritdoc />
    public async Task<Result<ApprovalActionResult>> RejectStepAsync(
        Guid stepId,
        string userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var step = await _approvalStepRepository.GetByIdForUpdateAsync(stepId, cancellationToken);
        if (step is null)
            return Result.Failure<ApprovalActionResult>("خطوة الاعتماد غير موجودة.");

        // Verify this step is currently actionable
        var currentPendingSteps = await _approvalStepRepository.GetCurrentPendingStepsAsync(
            step.CompetitionId, step.FromStatus, step.ToStatus, cancellationToken);

        if (!currentPendingSteps.Any(s => s.Id == stepId))
            return Result.Failure<ApprovalActionResult>(
                "هذه الخطوة ليست الخطوة الحالية في مسار الاعتماد.");

        // Reject the step
        var result = step.Reject(userId, reason);
        if (result.IsFailure)
            return Result.Failure<ApprovalActionResult>(result.Error!);

        _approvalStepRepository.Update(step);

        // Reset all subsequent steps back to pending
        var allSteps = await _approvalStepRepository.GetByCompetitionTransitionAsync(
            step.CompetitionId, step.FromStatus, step.ToStatus, cancellationToken);

        foreach (var subsequentStep in allSteps.Where(s => s.StepOrder > step.StepOrder))
        {
            var trackedStep = await _approvalStepRepository.GetByIdForUpdateAsync(
                subsequentStep.Id, cancellationToken);
            trackedStep?.Reset();
            if (trackedStep is not null)
                _approvalStepRepository.Update(trackedStep);
        }

        await _approvalStepRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(new ApprovalActionResult(
            StepId: stepId,
            CompetitionId: step.CompetitionId,
            IsWorkflowCompleted: false,
            FromStatus: step.FromStatus,
            ToStatus: step.ToStatus));
    }

    /// <inheritdoc />
    public async Task<ApprovalWorkflowStatusResult> GetWorkflowStatusAsync(
        Guid competitionId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        CancellationToken cancellationToken = default)
    {
        var steps = await _approvalStepRepository.GetByCompetitionTransitionAsync(
            competitionId, fromStatus, toStatus, cancellationToken);

        if (steps.Count == 0)
        {
            return new ApprovalWorkflowStatusResult(
                HasWorkflow: false,
                TotalSteps: 0,
                CompletedSteps: 0,
                CurrentStepOrder: 0,
                IsCompleted: false,
                IsRejected: false,
                Steps: []);
        }

        var completedSteps = steps.Count(s =>
            s.Status == ApprovalStepStatus.Approved || s.Status == ApprovalStepStatus.Skipped);
        var isRejected = steps.Any(s => s.Status == ApprovalStepStatus.Rejected);
        var isCompleted = completedSteps == steps.Count;

        var currentPending = steps
            .Where(s => s.Status == ApprovalStepStatus.Pending || s.Status == ApprovalStepStatus.InProgress)
            .Select(s => s.StepOrder)
            .DefaultIfEmpty(0)
            .Min();

        var stepDetails = steps.Select(s => new ApprovalStepDetail(
            StepId: s.Id,
            StepOrder: s.StepOrder,
            StepNameAr: s.StepNameAr,
            StepNameEn: s.StepNameEn,
            RequiredRole: s.RequiredRole,
            RequiredCommitteeRole: s.RequiredCommitteeRole,
            Status: s.Status,
            CompletedByUserId: s.CompletedByUserId,
            CompletedAt: s.CompletedAt,
            Comment: s.Comment,
            SlaDeadline: s.SlaDeadline,
            IsSlaExceeded: s.IsSlaExceeded)).ToList();

        return new ApprovalWorkflowStatusResult(
            HasWorkflow: true,
            TotalSteps: steps.Count,
            CompletedSteps: completedSteps,
            CurrentStepOrder: currentPending,
            IsCompleted: isCompleted,
            IsRejected: isRejected,
            Steps: stepDetails);
    }

    // ═════════════════════════════════════════════════════════════
    //  Fallback: Hardcoded Template (Backward Compatibility)
    // ═════════════════════════════════════════════════════════════

    private async Task<Result<ApprovalWorkflowInitiationResult>> InitiateFromHardcodedTemplateAsync(
        Guid competitionId,
        Guid tenantId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        string initiatedByUserId,
        CancellationToken cancellationToken)
    {
        // Use the existing ApprovalWorkflowTemplate for backward compatibility
        var templateSteps = ApprovalWorkflowTemplate.GetStepTemplates(fromStatus, toStatus);

        if (templateSteps.Count == 0)
            return Result.Failure<ApprovalWorkflowInitiationResult>(
                "لا يوجد مسار اعتماد معرّف لهذه المرحلة. يرجى إعداد مسار الاعتماد من إعدادات النظام.");

        var steps = new List<ApprovalWorkflowStep>();
        foreach (var template in templateSteps)
        {
            var step = ApprovalWorkflowStep.Create(
                competitionId: competitionId,
                tenantId: tenantId,
                fromStatus: fromStatus,
                toStatus: toStatus,
                stepOrder: template.Order,
                requiredRole: template.RequiredRole,
                requiredCommitteeRole: template.RequiredCommitteeRole,
                stepNameAr: template.StepNameAr,
                stepNameEn: template.StepNameEn,
                createdBy: initiatedByUserId);

            steps.Add(step);
        }

        await _approvalStepRepository.AddRangeAsync(steps, cancellationToken);
        await _approvalStepRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(new ApprovalWorkflowInitiationResult(
            WorkflowDefinitionId: null,
            TotalSteps: steps.Count,
            StepIds: steps.Select(s => s.Id).ToList()));
    }
}

// ═════════════════════════════════════════════════════════════
//  DTOs / Result Records
// ═════════════════════════════════════════════════════════════

/// <summary>
/// Result of initiating an approval workflow.
/// </summary>
public sealed record ApprovalWorkflowInitiationResult(
    Guid? WorkflowDefinitionId,
    int TotalSteps,
    IReadOnlyList<Guid> StepIds);

/// <summary>
/// Result of an approval or rejection action.
/// </summary>
public sealed record ApprovalActionResult(
    Guid StepId,
    Guid CompetitionId,
    bool IsWorkflowCompleted,
    CompetitionStatus FromStatus,
    CompetitionStatus ToStatus);

/// <summary>
/// Current status of an approval workflow for a competition transition.
/// </summary>
public sealed record ApprovalWorkflowStatusResult(
    bool HasWorkflow,
    int TotalSteps,
    int CompletedSteps,
    int CurrentStepOrder,
    bool IsCompleted,
    bool IsRejected,
    IReadOnlyList<ApprovalStepDetail> Steps);

/// <summary>
/// Detail of a single approval step.
/// </summary>
public sealed record ApprovalStepDetail(
    Guid StepId,
    int StepOrder,
    string StepNameAr,
    string StepNameEn,
    SystemRole RequiredRole,
    CommitteeRole RequiredCommitteeRole,
    ApprovalStepStatus Status,
    string? CompletedByUserId,
    DateTime? CompletedAt,
    string? Comment,
    DateTime? SlaDeadline,
    bool IsSlaExceeded);
