using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Workflow;

/// <summary>
/// Represents a single step within a <see cref="WorkflowDefinition"/>.
/// Steps define who must approve and in what order.
///
/// Parallelism is modeled by assigning the same <see cref="StepOrder"/>
/// to multiple steps — all steps at the same order must be completed
/// before the workflow advances to the next order.
///
/// Conditional steps are evaluated at runtime using <see cref="ConditionExpression"/>.
/// </summary>
public sealed class WorkflowStepDefinition : BaseEntity<Guid>
{
    private WorkflowStepDefinition() { } // EF Core constructor

    /// <summary>
    /// Creates a new workflow step definition.
    /// </summary>
    public static WorkflowStepDefinition Create(
        Guid workflowDefinitionId,
        int stepOrder,
        SystemRole requiredSystemRole,
        CommitteeRole requiredCommitteeRole,
        string stepNameAr,
        string stepNameEn,
        int? slaHours = null,
        bool isConditional = false,
        string? conditionExpression = null,
        string createdBy = "system")
    {
        return new WorkflowStepDefinition
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowDefinitionId,
            StepOrder = stepOrder,
            RequiredSystemRole = requiredSystemRole,
            RequiredCommitteeRole = requiredCommitteeRole,
            StepNameAr = stepNameAr,
            StepNameEn = stepNameEn,
            SlaHours = slaHours,
            IsConditional = isConditional,
            ConditionExpression = conditionExpression,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    // ═════════════════════════════════════════════════════════════
    //  Properties
    // ═════════════════════════════════════════════════════════════

    /// <summary>The workflow definition this step belongs to.</summary>
    public Guid WorkflowDefinitionId { get; private set; }

    /// <summary>
    /// The order of this step within the workflow (1-based).
    /// Steps with the same order are executed in parallel.
    /// Steps with different orders are executed sequentially.
    /// </summary>
    public int StepOrder { get; private set; }

    /// <summary>The system role required to complete this step.</summary>
    public SystemRole RequiredSystemRole { get; private set; }

    /// <summary>The committee role required (if any) to complete this step.</summary>
    public CommitteeRole RequiredCommitteeRole { get; private set; }

    /// <summary>Arabic name of this step.</summary>
    public string StepNameAr { get; private set; } = default!;

    /// <summary>English name of this step.</summary>
    public string StepNameEn { get; private set; } = default!;

    /// <summary>
    /// SLA deadline in hours for this step.
    /// Null means no SLA enforcement.
    /// </summary>
    public int? SlaHours { get; private set; }

    /// <summary>Whether this step is conditional (evaluated at runtime).</summary>
    public bool IsConditional { get; private set; }

    /// <summary>
    /// A simple condition expression evaluated at runtime to determine
    /// if this step should be included. Examples:
    /// - "EstimatedBudget > 1000000"
    /// - "CompetitionType == PublicTender"
    /// Null or empty means the step is always included.
    /// </summary>
    public string? ConditionExpression { get; private set; }

    /// <summary>Whether this step definition is active.</summary>
    public bool IsActive { get; private set; }

    // ═════════════════════════════════════════════════════════════
    //  Update Methods
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the step definition properties.
    /// </summary>
    public Result Update(
        int stepOrder,
        SystemRole requiredSystemRole,
        CommitteeRole requiredCommitteeRole,
        string stepNameAr,
        string stepNameEn,
        int? slaHours,
        bool isConditional,
        string? conditionExpression,
        string updatedBy)
    {
        if (stepOrder < 1)
            return Result.Failure("Step order must be at least 1.");

        StepOrder = stepOrder;
        RequiredSystemRole = requiredSystemRole;
        RequiredCommitteeRole = requiredCommitteeRole;
        StepNameAr = stepNameAr;
        StepNameEn = stepNameEn;
        SlaHours = slaHours;
        IsConditional = isConditional;
        ConditionExpression = conditionExpression;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = updatedBy;
        return Result.Success();
    }

    /// <summary>
    /// Activates this step definition.
    /// </summary>
    public void Activate(string activatedBy)
    {
        IsActive = true;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = activatedBy;
    }

    /// <summary>
    /// Deactivates this step definition.
    /// </summary>
    public void Deactivate(string deactivatedBy)
    {
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = deactivatedBy;
    }
}
