using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Represents a single step in the approval workflow for a competition phase transition.
/// Each phase may require multiple sequential approval steps from different roles.
/// Steps must be completed in order — no step can be skipped.
/// 
/// PRD Reference: Section 7.1 — مسار الاعتماد لكل مرحلة
/// </summary>
public sealed class ApprovalWorkflowStep : BaseEntity<Guid>
{
    private ApprovalWorkflowStep() { } // EF Core constructor

    public static ApprovalWorkflowStep Create(
        Guid competitionId,
        Guid tenantId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        int stepOrder,
        SystemRole requiredRole,
        CommitteeRole requiredCommitteeRole = CommitteeRole.None,
        string? stepNameAr = null,
        string? stepNameEn = null,
        string createdBy = "system")
    {
        return new ApprovalWorkflowStep
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            TenantId = tenantId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            StepOrder = stepOrder,
            RequiredRole = requiredRole,
            RequiredCommitteeRole = requiredCommitteeRole,
            StepNameAr = stepNameAr ?? $"خطوة الاعتماد {stepOrder}",
            StepNameEn = stepNameEn ?? $"Approval Step {stepOrder}",
            Status = ApprovalStepStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>The competition this approval step belongs to.</summary>
    public Guid CompetitionId { get; private set; }

    /// <summary>The tenant this approval step belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The source status of the transition this step guards.</summary>
    public CompetitionStatus FromStatus { get; private set; }

    /// <summary>The target status of the transition this step guards.</summary>
    public CompetitionStatus ToStatus { get; private set; }

    /// <summary>The order of this step within the approval workflow (1-based).</summary>
    public int StepOrder { get; private set; }

    /// <summary>The system role required to complete this step.</summary>
    public SystemRole RequiredRole { get; private set; }

    /// <summary>The committee role required (if any) to complete this step.</summary>
    public CommitteeRole RequiredCommitteeRole { get; private set; }

    /// <summary>Arabic name of this approval step.</summary>
    public string StepNameAr { get; private set; } = default!;

    /// <summary>English name of this approval step.</summary>
    public string StepNameEn { get; private set; } = default!;

    /// <summary>Current status of this approval step.</summary>
    public ApprovalStepStatus Status { get; private set; }

    /// <summary>User who completed this step.</summary>
    public string? CompletedByUserId { get; private set; }

    /// <summary>When this step was completed.</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Comment or note left by the approver.</summary>
    public string? Comment { get; private set; }

    /// <summary>
    /// Approves this step.
    /// </summary>
    public Result Approve(string userId, string? comment = null)
    {
        if (Status != ApprovalStepStatus.Pending && Status != ApprovalStepStatus.InProgress)
            return Result.Failure("This approval step is not in a pending or in-progress state.");

        Status = ApprovalStepStatus.Approved;
        CompletedByUserId = userId;
        CompletedAt = DateTime.UtcNow;
        Comment = comment;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = userId;

        return Result.Success();
    }

    /// <summary>
    /// Rejects this step.
    /// </summary>
    public Result Reject(string userId, string reason)
    {
        if (Status != ApprovalStepStatus.Pending && Status != ApprovalStepStatus.InProgress)
            return Result.Failure("This approval step is not in a pending or in-progress state.");

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("A reason is required for rejection.");

        Status = ApprovalStepStatus.Rejected;
        CompletedByUserId = userId;
        CompletedAt = DateTime.UtcNow;
        Comment = reason;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = userId;

        return Result.Success();
    }

    /// <summary>
    /// Marks this step as in-progress.
    /// </summary>
    public Result StartReview(string userId)
    {
        if (Status != ApprovalStepStatus.Pending)
            return Result.Failure("This approval step is not in a pending state.");

        Status = ApprovalStepStatus.InProgress;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = userId;

        return Result.Success();
    }

    /// <summary>
    /// Resets this step to pending (e.g., when a previous step is rejected and the workflow restarts).
    /// </summary>
    public void Reset()
    {
        Status = ApprovalStepStatus.Pending;
        CompletedByUserId = null;
        CompletedAt = null;
        Comment = null;
        LastModifiedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Status of an individual approval workflow step.
/// </summary>
public enum ApprovalStepStatus
{
    /// <summary>Waiting for action.</summary>
    Pending = 0,

    /// <summary>Currently being reviewed.</summary>
    InProgress = 1,

    /// <summary>Approved by the required role.</summary>
    Approved = 2,

    /// <summary>Rejected by the required role.</summary>
    Rejected = 3,

    /// <summary>Skipped (not applicable for this competition).</summary>
    Skipped = 4
}
