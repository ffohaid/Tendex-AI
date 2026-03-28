using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Committees;

/// <summary>
/// Represents a member assigned to a committee with a specific role.
/// Maps to PRD Section 4.3 — committee member management.
/// 
/// Each member has a role (Chair, Member, Secretary) and an optional
/// phase range during which their membership is active.
/// </summary>
public sealed class CommitteeMember : BaseEntity<Guid>
{
    /// <summary>Required for EF Core.</summary>
    private CommitteeMember() { }

    /// <summary>
    /// Creates a new committee member assignment.
    /// </summary>
    public CommitteeMember(
        Guid committeeId,
        Guid userId,
        string userFullName,
        CommitteeMemberRole role,
        CompetitionPhase? activeFromPhase,
        CompetitionPhase? activeToPhase,
        string assignedBy)
    {
        Id = Guid.NewGuid();
        CommitteeId = committeeId;
        UserId = userId;
        UserFullName = userFullName;
        Role = role;
        ActiveFromPhase = activeFromPhase;
        ActiveToPhase = activeToPhase;
        IsActive = true;
        AssignedAt = DateTime.UtcNow;
        AssignedBy = assignedBy;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = assignedBy;
    }

    // ═════════════════════════════════════════════════════════════
    //  Properties
    // ═════════════════════════════════════════════════════════════

    /// <summary>The committee this member belongs to.</summary>
    public Guid CommitteeId { get; private set; }

    /// <summary>The user (ApplicationUser) assigned to this committee.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Cached full name of the user for display purposes.</summary>
    public string UserFullName { get; private set; } = default!;

    /// <summary>The role of this member within the committee.</summary>
    public CommitteeMemberRole Role { get; private set; }

    /// <summary>
    /// The phase from which this membership becomes active.
    /// Null means active from the beginning.
    /// </summary>
    public CompetitionPhase? ActiveFromPhase { get; private set; }

    /// <summary>
    /// The phase until which this membership remains active.
    /// Null means active until the end.
    /// </summary>
    public CompetitionPhase? ActiveToPhase { get; private set; }

    /// <summary>Whether this membership is currently active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>When the user was assigned to this committee.</summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>Who assigned this user to the committee.</summary>
    public string AssignedBy { get; private set; } = default!;

    /// <summary>When the user was removed from the committee (if applicable).</summary>
    public DateTime? RemovedAt { get; private set; }

    /// <summary>Who removed the user from the committee.</summary>
    public string? RemovedBy { get; private set; }

    /// <summary>Reason for removal (audit trail).</summary>
    public string? RemovalReason { get; private set; }

    /// <summary>Navigation property — the parent committee.</summary>
    public Committee Committee { get; private set; } = default!;

    // ═════════════════════════════════════════════════════════════
    //  Methods
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Checks if this membership is active for a given competition phase.
    /// </summary>
    public bool IsActiveForPhase(CompetitionPhase phase)
    {
        if (!IsActive) return false;
        var fromOk = !ActiveFromPhase.HasValue || phase >= ActiveFromPhase.Value;
        var toOk = !ActiveToPhase.HasValue || phase <= ActiveToPhase.Value;
        return fromOk && toOk;
    }

    /// <summary>
    /// Deactivates this committee membership with audit information.
    /// </summary>
    public void Deactivate(string removedBy, string reason)
    {
        IsActive = false;
        RemovedAt = DateTime.UtcNow;
        RemovedBy = removedBy;
        RemovalReason = reason;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = removedBy;
    }

    /// <summary>
    /// Reactivates this committee membership.
    /// </summary>
    public void Reactivate(string reactivatedBy)
    {
        IsActive = true;
        RemovedAt = null;
        RemovedBy = null;
        RemovalReason = null;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = reactivatedBy;
    }

    /// <summary>
    /// Updates the role of this member within the committee.
    /// </summary>
    public void UpdateRole(CommitteeMemberRole newRole, string updatedBy)
    {
        Role = newRole;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = updatedBy;
    }
}
