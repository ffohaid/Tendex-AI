using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Represents a user's membership in a committee for a specific competition.
/// This is the link between a user, a competition, and their committee role —
/// the first dimension (Competition) of the 4D permissions matrix is resolved here.
/// </summary>
public sealed class CompetitionCommitteeMember : BaseEntity<Guid>
{
    private CompetitionCommitteeMember() { } // EF Core constructor

    public static CompetitionCommitteeMember Create(
        Guid competitionId,
        Guid tenantId,
        string userId,
        CommitteeRole committeeRole,
        CompetitionPhase? activeFromPhase = null,
        CompetitionPhase? activeToPhase = null,
        string createdBy = "system")
    {
        return new CompetitionCommitteeMember
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            TenantId = tenantId,
            UserId = userId,
            CommitteeRole = committeeRole,
            ActiveFromPhase = activeFromPhase,
            ActiveToPhase = activeToPhase,
            IsActive = true,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>The competition this membership belongs to.</summary>
    public Guid CompetitionId { get; private set; }

    /// <summary>The tenant this membership belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The user ID of the committee member.</summary>
    public string UserId { get; private set; } = default!;

    /// <summary>The role of the user within the committee.</summary>
    public CommitteeRole CommitteeRole { get; private set; }

    /// <summary>
    /// The phase from which this committee membership becomes active.
    /// Null means active from the beginning.
    /// </summary>
    public CompetitionPhase? ActiveFromPhase { get; private set; }

    /// <summary>
    /// The phase until which this committee membership remains active.
    /// Null means active until the end.
    /// </summary>
    public CompetitionPhase? ActiveToPhase { get; private set; }

    /// <summary>Whether this membership is currently active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>When the user was assigned to this committee.</summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>When the user was removed from this committee (if applicable).</summary>
    public DateTime? RemovedAt { get; private set; }

    /// <summary>Who removed the user from the committee.</summary>
    public string? RemovedBy { get; private set; }

    /// <summary>Reason for removal (if applicable).</summary>
    public string? RemovalReason { get; private set; }

    /// <summary>
    /// Checks if this membership is active for a given phase.
    /// </summary>
    public bool IsActiveForPhase(CompetitionPhase phase)
    {
        if (!IsActive) return false;

        var fromOk = !ActiveFromPhase.HasValue || phase >= ActiveFromPhase.Value;
        var toOk = !ActiveToPhase.HasValue || phase <= ActiveToPhase.Value;

        return fromOk && toOk;
    }

    /// <summary>
    /// Deactivates this committee membership.
    /// </summary>
    public void Deactivate(string removedBy, string? reason = null)
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
}
