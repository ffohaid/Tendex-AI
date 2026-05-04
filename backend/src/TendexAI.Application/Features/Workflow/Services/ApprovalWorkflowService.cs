using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
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
/// 2. Processing approval/rejection actions on individual steps with role authorization.
/// 3. Advancing the workflow when all steps at a given order are completed.
/// 4. Triggering competition state transitions when all steps are completed.
/// 5. Evaluating conditional step expressions at initiation time.
/// </summary>
public sealed class ApprovalWorkflowService : IApprovalWorkflowService
{
    private readonly IWorkflowDefinitionRepository _workflowDefinitionRepository;
    private readonly IApprovalWorkflowStepRepository _approvalStepRepository;
    private readonly ICompetitionRepository _competitionRepository;
    private readonly IWorkflowConditionEvaluator _conditionEvaluator;
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ICurrentUserService _currentUser;

    public ApprovalWorkflowService(
        IWorkflowDefinitionRepository workflowDefinitionRepository,
        IApprovalWorkflowStepRepository approvalStepRepository,
        ICompetitionRepository competitionRepository,
        IWorkflowConditionEvaluator conditionEvaluator,
        ITenantDbContextFactory tenantDbContextFactory,
        ICurrentUserService currentUser)
    {
        _workflowDefinitionRepository = workflowDefinitionRepository;
        _approvalStepRepository = approvalStepRepository;
        _competitionRepository = competitionRepository;
        _conditionEvaluator = conditionEvaluator;
        _tenantDbContextFactory = tenantDbContextFactory;
        _currentUser = currentUser;
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

        // 3. Get competition context for condition evaluation
        var competition = await _competitionRepository.GetByIdForUpdateAsync(competitionId, cancellationToken);
        var conditionContext = competition is not null
            ? _conditionEvaluator.BuildContext(competition)
            : new Dictionary<string, object>();

        // 4. Create runtime approval steps from the definition
        var steps = new List<ApprovalWorkflowStep>();
        var skippedConditionalSteps = 0;

        foreach (var stepDef in definition.Steps.Where(s => s.IsActive).OrderBy(s => s.StepOrder))
        {
            // Evaluate conditional steps using ConditionExpression
            if (stepDef.IsConditional && !string.IsNullOrEmpty(stepDef.ConditionExpression))
            {
                var conditionMet = _conditionEvaluator.Evaluate(
                    stepDef.ConditionExpression, conditionContext);

                if (!conditionMet)
                {
                    skippedConditionalSteps++;
                    continue; // Skip this step — condition not met
                }
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
        IReadOnlyCollection<string>? roleIdentifiers,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var step = await _approvalStepRepository.GetByIdForUpdateAsync(stepId, cancellationToken);
        if (step is null)
            return Result.Failure<ApprovalActionResult>("خطوة الاعتماد غير موجودة.");

        var currentPendingSteps = await _approvalStepRepository.GetCurrentPendingStepsAsync(
            step.CompetitionId, step.FromStatus, step.ToStatus, cancellationToken);

        if (!currentPendingSteps.Any(s => s.Id == stepId))
            return Result.Failure<ApprovalActionResult>(
                "هذه الخطوة ليست الخطوة الحالية في مسار الاعتماد.");

        var authorizationContext = await BuildAuthorizationContextAsync(
            step.CompetitionId,
            roleIdentifiers,
            userId,
            cancellationToken);

        var roleAuthResult = ValidateStepAuthorization(step, authorizationContext.SystemRoles, authorizationContext.CommitteeRoles);
        if (roleAuthResult.IsFailure)
            return Result.Failure<ApprovalActionResult>(roleAuthResult.Error!);

        return await ApproveStepAsync(stepId, userId, comment, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<ApprovalActionResult>> ApproveStepAsync(
        Guid stepId,
        string userId,
        SystemRole userSystemRole,
        CommitteeRole userCommitteeRole,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var step = await _approvalStepRepository.GetByIdForUpdateAsync(stepId, cancellationToken);
        if (step is null)
            return Result.Failure<ApprovalActionResult>("خطوة الاعتماد غير موجودة.");

        var currentPendingSteps = await _approvalStepRepository.GetCurrentPendingStepsAsync(
            step.CompetitionId, step.FromStatus, step.ToStatus, cancellationToken);

        if (!currentPendingSteps.Any(s => s.Id == stepId))
            return Result.Failure<ApprovalActionResult>(
                "هذه الخطوة ليست الخطوة الحالية في مسار الاعتماد.");

        var systemRoles = new[] { userSystemRole };
        var committeeRoles = userCommitteeRole == CommitteeRole.None
            ? Array.Empty<CommitteeRole>()
            : new[] { userCommitteeRole };

        var roleAuthResult = ValidateStepAuthorization(step, systemRoles, committeeRoles);
        if (roleAuthResult.IsFailure)
            return Result.Failure<ApprovalActionResult>(roleAuthResult.Error!);

        return await ApproveStepAsync(stepId, userId, comment, cancellationToken);
    }

    /// <summary>
    /// Backward-compatible overload without role parameters.
    /// Uses permissive authorization (any authenticated user can approve).
    /// </summary>
    public async Task<Result<ApprovalActionResult>> ApproveStepAsync(
        Guid stepId,
        string userId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        // Permissive mode — skip role validation for backward compatibility
        var step = await _approvalStepRepository.GetByIdForUpdateAsync(stepId, cancellationToken);
        if (step is null)
            return Result.Failure<ApprovalActionResult>("خطوة الاعتماد غير موجودة.");

        var currentPendingSteps = await _approvalStepRepository.GetCurrentPendingStepsAsync(
            step.CompetitionId, step.FromStatus, step.ToStatus, cancellationToken);

        if (!currentPendingSteps.Any(s => s.Id == stepId))
            return Result.Failure<ApprovalActionResult>(
                "هذه الخطوة ليست الخطوة الحالية في مسار الاعتماد.");

        var result = step.Approve(userId, comment);
        if (result.IsFailure)
            return Result.Failure<ApprovalActionResult>(result.Error!);

        _approvalStepRepository.Update(step);
        await _approvalStepRepository.SaveChangesAsync(cancellationToken);

        var allStepsCompleted = await _approvalStepRepository.AreAllStepsCompletedAsync(
            step.CompetitionId, step.FromStatus, step.ToStatus, cancellationToken);

        // Auto-transition competition status when all steps are completed
        if (allStepsCompleted)
        {
            await TransitionCompetitionStatusAsync(
                step.CompetitionId, step.ToStatus, userId, cancellationToken);
        }

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
        IReadOnlyCollection<string>? roleIdentifiers,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var step = await _approvalStepRepository.GetByIdForUpdateAsync(stepId, cancellationToken);
        if (step is null)
            return Result.Failure<ApprovalActionResult>("خطوة الاعتماد غير موجودة.");

        var currentPendingSteps = await _approvalStepRepository.GetCurrentPendingStepsAsync(
            step.CompetitionId, step.FromStatus, step.ToStatus, cancellationToken);

        if (!currentPendingSteps.Any(s => s.Id == stepId))
            return Result.Failure<ApprovalActionResult>(
                "هذه الخطوة ليست الخطوة الحالية في مسار الاعتماد.");

        var authorizationContext = await BuildAuthorizationContextAsync(
            step.CompetitionId,
            roleIdentifiers,
            userId,
            cancellationToken);

        var roleAuthResult = ValidateStepAuthorization(step, authorizationContext.SystemRoles, authorizationContext.CommitteeRoles);
        if (roleAuthResult.IsFailure)
            return Result.Failure<ApprovalActionResult>(roleAuthResult.Error!);

        return await RejectStepAsync(stepId, userId, reason, cancellationToken);
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

        var currentPendingSteps = await _approvalStepRepository.GetCurrentPendingStepsAsync(
            step.CompetitionId, step.FromStatus, step.ToStatus, cancellationToken);

        if (!currentPendingSteps.Any(s => s.Id == stepId))
            return Result.Failure<ApprovalActionResult>(
                "هذه الخطوة ليست الخطوة الحالية في مسار الاعتماد.");

        var result = step.Reject(userId, reason);
        if (result.IsFailure)
            return Result.Failure<ApprovalActionResult>(result.Error!);

        _approvalStepRepository.Update(step);

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
        await RevertCompetitionAfterRejectionAsync(step.CompetitionId, step.FromStatus, step.ToStatus, userId, reason, cancellationToken);

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

        var userSystemRoles = ApprovalActorResolver.ResolveSystemRoles(_currentUser.Roles).ToHashSet();
        var userCommitteeRoles = new HashSet<CommitteeRole>();

        if (_currentUser.UserId.HasValue)
        {
            var dbContext = _tenantDbContextFactory.CreateDbContext();
            userCommitteeRoles = (await ApprovalActorResolver.ResolveCommitteeRolesForCompetitionAsync(
                dbContext,
                competitionId,
                _currentUser.UserId.Value,
                cancellationToken)).ToHashSet();

            if (userCommitteeRoles.Count > 0 && !userSystemRoles.Contains(SystemRole.Member))
            {
                userSystemRoles.Add(SystemRole.Member);
            }
        }

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
            IsSlaExceeded: s.IsSlaExceeded,
            CanCurrentUserAct: ApprovalStepAccessEvaluator.CanCurrentUserAct(
                s,
                currentPending,
                isCompleted,
                isRejected,
                userSystemRoles,
                userCommitteeRoles))).ToList();

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
    //  Authorization Validation
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates that the user has the required system role and committee role
    /// to act on the given approval step.
    /// </summary>
    private static Result ValidateStepAuthorization(
        ApprovalWorkflowStep step,
        IReadOnlyCollection<SystemRole> userSystemRoles,
        IReadOnlyCollection<CommitteeRole> userCommitteeRoles)
    {
        if (!userSystemRoles.Contains(step.RequiredRole))
        {
            var currentRoles = userSystemRoles.Count == 0
                ? "بدون أدوار مطابقة"
                : string.Join(", ", userSystemRoles);

            return Result.Failure(
                $"ليس لديك الصلاحية المطلوبة لاعتماد هذه الخطوة. الدور المطلوب: {step.RequiredRole}، أدوارك الحالية: {currentRoles}.");
        }

        if (step.RequiredCommitteeRole != CommitteeRole.None &&
            !userCommitteeRoles.Contains(step.RequiredCommitteeRole))
        {
            var currentCommitteeRoles = userCommitteeRoles.Count == 0
                ? "بدون عضوية لجنة مطابقة"
                : string.Join(", ", userCommitteeRoles);

            return Result.Failure(
                $"يجب أن تكون عضواً في اللجنة المطلوبة لاعتماد هذه الخطوة. الدور المطلوب: {step.RequiredCommitteeRole}، عضوياتك الحالية: {currentCommitteeRoles}.");
        }

        return Result.Success();
    }

    // ═════════════════════════════════════════════════════════════
    //  Competition Status Transition
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Automatically transitions the competition status when all approval steps
    /// are completed. This ensures the workflow engine drives the competition
    /// lifecycle forward without manual intervention.
    /// </summary>
    private async Task<ApprovalAuthorizationContext> BuildAuthorizationContextAsync(
        Guid competitionId,
        IReadOnlyCollection<string>? roleIdentifiers,
        string userId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var parsedUserId))
            return new ApprovalAuthorizationContext(Array.Empty<SystemRole>(), Array.Empty<CommitteeRole>());

        var dbContext = _tenantDbContextFactory.CreateDbContext();
        var systemRoles = ApprovalActorResolver.ResolveSystemRoles(roleIdentifiers).ToHashSet();
        var committeeRoles = (await ApprovalActorResolver.ResolveCommitteeRolesForCompetitionAsync(
                dbContext,
                competitionId,
                parsedUserId,
                cancellationToken))
            .ToHashSet();

        if (committeeRoles.Count > 0)
            systemRoles.Add(SystemRole.Member);

        return new ApprovalAuthorizationContext(systemRoles.ToArray(), committeeRoles.ToArray());
    }

    private async Task TransitionCompetitionStatusAsync(
        Guid competitionId,
        CompetitionStatus targetStatus,
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var competition = await _competitionRepository.GetByIdForUpdateAsync(competitionId, cancellationToken);
            if (competition is null) return;

            // Only transition if the competition is not already at or past the target status
            if ((int)competition.Status < (int)targetStatus)
            {
                var transitionResult = competition.TransitionTo(targetStatus, userId);
                if (transitionResult.IsSuccess)
                {
                    _competitionRepository.Update(competition);
                    await _competitionRepository.SaveChangesAsync(cancellationToken);
                }
            }
        }
        catch (Exception)
        {
            // Log but don't fail the approval action if status transition fails
            // The workflow is still marked as completed
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  Fallback: Hardcoded Template (Backward Compatibility)
    // ═════════════════════════════════════════════════════════════

    private async Task RevertCompetitionAfterRejectionAsync(
        Guid competitionId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        string userId,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var competition = await _competitionRepository.GetByIdForUpdateAsync(competitionId, cancellationToken);
            if (competition is null)
                return;

            var targetStatus = fromStatus == CompetitionStatus.PendingApproval && toStatus == CompetitionStatus.Approved
                ? CompetitionStatus.UnderPreparation
                : fromStatus;

            if (competition.Status != fromStatus)
                return;

            var transitionResult = competition.TransitionTo(targetStatus, userId, reason);
            if (transitionResult.IsSuccess)
            {
                _competitionRepository.Update(competition);
                await _competitionRepository.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            // The workflow rejection remains persisted even if the competition transition fails.
        }
    }

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

    private sealed record ApprovalAuthorizationContext(
        IReadOnlyCollection<SystemRole> SystemRoles,
        IReadOnlyCollection<CommitteeRole> CommitteeRoles);
}

// ═════════════════════════════════════════════════════════════
//  Condition Evaluator Interface & Implementation
// ═════════════════════════════════════════════════════════════

/// <summary>
/// Evaluates conditional expressions for workflow step definitions.
/// Supports simple comparison expressions like "EstimatedValue > 1000000"
/// and "CompetitionType == PublicTender".
/// </summary>
public interface IWorkflowConditionEvaluator
{
    /// <summary>
    /// Builds a context dictionary from a competition entity for condition evaluation.
    /// </summary>
    Dictionary<string, object> BuildContext(object competition);

    /// <summary>
    /// Evaluates a condition expression against the provided context.
    /// Returns true if the condition is met, false otherwise.
    /// </summary>
    bool Evaluate(string expression, Dictionary<string, object> context);
}

/// <summary>
/// Default implementation of the condition evaluator.
/// Supports simple binary comparison expressions:
/// - Numeric comparisons: "EstimatedValue > 1000000", "EstimatedValue >= 500000"
/// - Equality checks: "CompetitionType == PublicTender", "CompetitionType != DirectPurchase"
/// </summary>
public sealed class SimpleWorkflowConditionEvaluator : IWorkflowConditionEvaluator
{
    public Dictionary<string, object> BuildContext(object competition)
    {
        var context = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Use reflection to extract common competition properties
        var type = competition.GetType();

        var estimatedValue = type.GetProperty("EstimatedValue")?.GetValue(competition);
        if (estimatedValue is not null)
            context["EstimatedValue"] = estimatedValue;

        var competitionType = type.GetProperty("CompetitionType")?.GetValue(competition);
        if (competitionType is not null)
            context["CompetitionType"] = competitionType.ToString()!;

        var status = type.GetProperty("Status")?.GetValue(competition);
        if (status is not null)
            context["Status"] = status.ToString()!;

        var evaluationMethod = type.GetProperty("EvaluationMethod")?.GetValue(competition);
        if (evaluationMethod is not null)
            context["EvaluationMethod"] = evaluationMethod.ToString()!;

        return context;
    }

    public bool Evaluate(string expression, Dictionary<string, object> context)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return true; // Empty expression always passes

        try
        {
            // Parse simple binary expressions: "PropertyName operator Value"
            var parts = expression.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                return true; // Cannot parse — include the step by default

            var propertyName = parts[0];
            var op = parts[1];
            var valueStr = parts[2];

            if (!context.TryGetValue(propertyName, out var contextValue))
                return true; // Property not found — include the step by default

            // Numeric comparison
            if (decimal.TryParse(valueStr, out var numericValue) &&
                contextValue is IConvertible convertible)
            {
                try
                {
                    var contextNumeric = Convert.ToDecimal(convertible, System.Globalization.CultureInfo.InvariantCulture);
                    return op switch
                    {
                        ">" => contextNumeric > numericValue,
                        ">=" => contextNumeric >= numericValue,
                        "<" => contextNumeric < numericValue,
                        "<=" => contextNumeric <= numericValue,
                        "==" => contextNumeric == numericValue,
                        "!=" => contextNumeric != numericValue,
                        _ => true
                    };
                }
                catch
                {
                    return true;
                }
            }

            // String equality comparison
            var contextStr = contextValue.ToString() ?? "";
            return op switch
            {
                "==" => string.Equals(contextStr, valueStr, StringComparison.OrdinalIgnoreCase),
                "!=" => !string.Equals(contextStr, valueStr, StringComparison.OrdinalIgnoreCase),
                _ => true
            };
        }
        catch
        {
            // If evaluation fails, include the step by default (fail-open for safety)
            return true;
        }
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
    bool IsSlaExceeded,
    bool CanCurrentUserAct);
