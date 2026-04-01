using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Workflow;

/// <summary>
/// Represents a configurable workflow definition (template) for a specific
/// competition phase transition. Each tenant can customize the approval steps
/// for each transition through the admin settings UI.
///
/// This entity replaces the hardcoded <c>ApprovalWorkflowTemplate</c> with
/// a database-driven, dynamic workflow engine (PRD Section 6).
/// </summary>
public sealed class WorkflowDefinition : BaseEntity<Guid>
{
    private readonly List<WorkflowStepDefinition> _steps = [];

    private WorkflowDefinition() { } // EF Core constructor

    /// <summary>
    /// Creates a new workflow definition for a specific phase transition.
    /// </summary>
    public static WorkflowDefinition Create(
        Guid tenantId,
        string nameAr,
        string nameEn,
        CompetitionStatus transitionFrom,
        CompetitionStatus transitionTo,
        string? descriptionAr = null,
        string? descriptionEn = null,
        string createdBy = "system")
    {
        return new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NameAr = nameAr,
            NameEn = nameEn,
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            TransitionFrom = transitionFrom,
            TransitionTo = transitionTo,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    // ═════════════════════════════════════════════════════════════
    //  Properties
    // ═════════════════════════════════════════════════════════════

    /// <summary>The tenant this workflow definition belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Arabic name of the workflow (e.g., "مسار اعتماد الكراسة").</summary>
    public string NameAr { get; private set; } = default!;

    /// <summary>English name of the workflow (e.g., "Booklet Approval Workflow").</summary>
    public string NameEn { get; private set; } = default!;

    /// <summary>Arabic description of the workflow.</summary>
    public string? DescriptionAr { get; private set; }

    /// <summary>English description of the workflow.</summary>
    public string? DescriptionEn { get; private set; }

    /// <summary>The source status of the transition this workflow governs.</summary>
    public CompetitionStatus TransitionFrom { get; private set; }

    /// <summary>The target status of the transition this workflow governs.</summary>
    public CompetitionStatus TransitionTo { get; private set; }

    /// <summary>Whether this workflow definition is currently active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Version counter for optimistic concurrency.</summary>
    public int Version { get; private set; }

    /// <summary>The step definitions that compose this workflow.</summary>
    public IReadOnlyList<WorkflowStepDefinition> Steps => _steps.AsReadOnly();

    // ═════════════════════════════════════════════════════════════
    //  Step Management
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds a step definition to this workflow.
    /// Steps with the same <paramref name="stepOrder"/> are treated as parallel.
    /// </summary>
    public Result AddStep(
        int stepOrder,
        SystemRole requiredSystemRole,
        CommitteeRole requiredCommitteeRole,
        string stepNameAr,
        string stepNameEn,
        int? slaHours = null,
        bool isConditional = false,
        string? conditionExpression = null,
        string addedBy = "system")
    {
        if (stepOrder < 1)
            return Result.Failure("Step order must be at least 1.");

        if (string.IsNullOrWhiteSpace(stepNameAr))
            return Result.Failure("Arabic step name is required.");

        var step = WorkflowStepDefinition.Create(
            workflowDefinitionId: Id,
            stepOrder: stepOrder,
            requiredSystemRole: requiredSystemRole,
            requiredCommitteeRole: requiredCommitteeRole,
            stepNameAr: stepNameAr,
            stepNameEn: stepNameEn,
            slaHours: slaHours,
            isConditional: isConditional,
            conditionExpression: conditionExpression,
            createdBy: addedBy);

        _steps.Add(step);
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = addedBy;
        return Result.Success();
    }

    /// <summary>
    /// Removes a step definition from this workflow.
    /// </summary>
    public Result RemoveStep(Guid stepId, string removedBy)
    {
        var step = _steps.FirstOrDefault(s => s.Id == stepId);
        if (step is null)
            return Result.Failure("Step not found in this workflow definition.");

        _steps.Remove(step);
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = removedBy;
        return Result.Success();
    }

    /// <summary>
    /// Removes all step definitions from this workflow (used for full replacement updates).
    /// </summary>
    public void ClearSteps()
    {
        _steps.Clear();
        LastModifiedAt = DateTime.UtcNow;
    }

    // ═════════════════════════════════════════════════════════════
    //  Lifecycle Management
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the workflow definition metadata.
    /// </summary>
    public Result UpdateInfo(
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure("Arabic name is required.");

        NameAr = nameAr;
        NameEn = nameEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = updatedBy;
        return Result.Success();
    }

    /// <summary>
    /// Activates this workflow definition.
    /// </summary>
    public void Activate(string activatedBy)
    {
        IsActive = true;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = activatedBy;
    }

    /// <summary>
    /// Deactivates this workflow definition.
    /// </summary>
    public void Deactivate(string deactivatedBy)
    {
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = deactivatedBy;
    }

    /// <summary>
    /// Increments the version for optimistic concurrency.
    /// </summary>
    public void IncrementVersion()
    {
        Version++;
    }

    // ═════════════════════════════════════════════════════════════
    //  Query Helpers
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the ordered steps grouped by step order.
    /// Steps with the same order are parallel; different orders are sequential.
    /// </summary>
    public IReadOnlyList<IGrouping<int, WorkflowStepDefinition>> GetOrderedStepGroups()
    {
        return _steps
            .OrderBy(s => s.StepOrder)
            .GroupBy(s => s.StepOrder)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets the total number of sequential stages (distinct step orders).
    /// </summary>
    public int SequentialStageCount => _steps
        .Select(s => s.StepOrder)
        .Distinct()
        .Count();
}
