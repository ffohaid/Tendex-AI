using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Committees;

/// <summary>
/// Aggregate root representing an evaluation committee (permanent or temporary).
/// Maps to PRD Section 4 — Committee and Institutional Governance System.
/// 
/// Permanent committees are created by the system admin and persist across competitions.
/// Temporary committees are formed per competition and dissolve when the task is complete.
/// 
/// CRITICAL BUSINESS RULE (PRD Section 4.2):
/// Technical evaluation committee must be completely separate from financial evaluation committee.
/// </summary>
public sealed class Committee : AggregateRoot<Guid>
{
    private readonly List<CommitteeMember> _members = [];

    /// <summary>Required for EF Core.</summary>
    private Committee() { }

    /// <summary>
    /// Creates a new committee instance.
    /// </summary>
    public Committee(
        Guid tenantId,
        string nameAr,
        string nameEn,
        CommitteeType type,
        bool isPermanent,
        string? description,
        DateTime startDate,
        DateTime endDate,
        Guid? competitionId,
        CompetitionPhase? activeFromPhase,
        CompetitionPhase? activeToPhase,
        string createdBy)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        NameAr = nameAr;
        NameEn = nameEn;
        Type = type;
        IsPermanent = isPermanent;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        CompetitionId = competitionId;
        ActiveFromPhase = activeFromPhase;
        ActiveToPhase = activeToPhase;
        Status = CommitteeStatus.Active;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    // ═════════════════════════════════════════════════════════════
    //  Properties
    // ═════════════════════════════════════════════════════════════

    /// <summary>The tenant (government entity) this committee belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Arabic name of the committee.</summary>
    public string NameAr { get; private set; } = default!;

    /// <summary>English name of the committee.</summary>
    public string NameEn { get; private set; } = default!;

    /// <summary>The type of this committee (Technical, Financial, Preparation, etc.).</summary>
    public CommitteeType Type { get; private set; }

    /// <summary>Whether this is a permanent committee (true) or temporary (false).</summary>
    public bool IsPermanent { get; private set; }

    /// <summary>Optional description of the committee's mandate.</summary>
    public string? Description { get; private set; }

    /// <summary>When the committee's term begins.</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>When the committee's term ends.</summary>
    public DateTime EndDate { get; private set; }

    /// <summary>Current lifecycle status of the committee.</summary>
    public CommitteeStatus Status { get; private set; }

    /// <summary>
    /// The competition this committee is linked to (null for permanent committees).
    /// Temporary committees are always linked to a specific competition.
    /// </summary>
    public Guid? CompetitionId { get; private set; }

    /// <summary>The phase from which this committee becomes active (for temporary committees).</summary>
    public CompetitionPhase? ActiveFromPhase { get; private set; }

    /// <summary>The phase until which this committee remains active (for temporary committees).</summary>
    public CompetitionPhase? ActiveToPhase { get; private set; }

    /// <summary>Reason for suspension or dissolution (audit trail).</summary>
    public string? StatusChangeReason { get; private set; }

    /// <summary>Who changed the status last.</summary>
    public string? StatusChangedBy { get; private set; }

    /// <summary>When the status was last changed.</summary>
    public DateTime? StatusChangedAt { get; private set; }

    /// <summary>Navigation property — committee members.</summary>
    public IReadOnlyCollection<CommitteeMember> Members => _members.AsReadOnly();

    // ═════════════════════════════════════════════════════════════
    //  Member Management
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds a member to the committee with a specified role.
    /// Enforces business rule: only one Chair per committee.
    /// </summary>
    public Result AddMember(
        Guid userId,
        string userFullName,
        CommitteeMemberRole role,
        CompetitionPhase? activeFromPhase,
        CompetitionPhase? activeToPhase,
        string assignedBy)
    {
        if (Status != CommitteeStatus.Active)
            return Result.Failure("Cannot add members to a non-active committee.");

        // Business rule: only one active Chair per committee
        if (role == CommitteeMemberRole.Chair &&
            _members.Any(m => m.Role == CommitteeMemberRole.Chair && m.IsActive))
            return Result.Failure("Committee already has an active chair. Remove or deactivate the current chair first.");

        // Business rule: prevent duplicate active membership
        if (_members.Any(m => m.UserId == userId && m.IsActive))
            return Result.Failure("User is already an active member of this committee.");

        var member = new CommitteeMember(
            Id, userId, userFullName, role,
            activeFromPhase, activeToPhase, assignedBy);

        _members.Add(member);
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = assignedBy;
        return Result.Success();
    }

    /// <summary>
    /// Removes (deactivates) a member from the committee with an audit reason.
    /// </summary>
    public Result RemoveMember(Guid userId, string removedBy, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("A reason is required for removing a committee member.");

        var member = _members.FirstOrDefault(m => m.UserId == userId && m.IsActive);
        if (member is null)
            return Result.Failure("Active member not found in this committee.");

        member.Deactivate(removedBy, reason);
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = removedBy;
        return Result.Success();
    }

    /// <summary>
    /// Checks if a user is an active member of this committee.
    /// </summary>
    public bool HasActiveMember(Guid userId)
    {
        return _members.Any(m => m.UserId == userId && m.IsActive);
    }

    /// <summary>
    /// Checks if a user is the active chair of this committee.
    /// </summary>
    public bool IsChair(Guid userId)
    {
        return _members.Any(m => m.UserId == userId && m.Role == CommitteeMemberRole.Chair && m.IsActive);
    }

    /// <summary>
    /// Gets the active chair of this committee (if any).
    /// </summary>
    public CommitteeMember? GetActiveChair()
    {
        return _members.FirstOrDefault(m => m.Role == CommitteeMemberRole.Chair && m.IsActive);
    }

    /// <summary>
    /// Gets all active members for a given competition phase.
    /// </summary>
    public IReadOnlyList<CommitteeMember> GetActiveMembersForPhase(CompetitionPhase phase)
    {
        return _members
            .Where(m => m.IsActive && m.IsActiveForPhase(phase))
            .ToList()
            .AsReadOnly();
    }

    // ═════════════════════════════════════════════════════════════
    //  Lifecycle Management
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Suspends the committee with a documented reason.
    /// </summary>
    public Result Suspend(string suspendedBy, string reason)
    {
        if (Status != CommitteeStatus.Active)
            return Result.Failure("Only active committees can be suspended.");

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("A reason is required for suspending a committee.");

        Status = CommitteeStatus.Suspended;
        StatusChangeReason = reason;
        StatusChangedBy = suspendedBy;
        StatusChangedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = suspendedBy;
        return Result.Success();
    }

    /// <summary>
    /// Reactivates a suspended committee.
    /// </summary>
    public Result Reactivate(string reactivatedBy)
    {
        if (Status != CommitteeStatus.Suspended)
            return Result.Failure("Only suspended committees can be reactivated.");

        Status = CommitteeStatus.Active;
        StatusChangeReason = null;
        StatusChangedBy = reactivatedBy;
        StatusChangedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = reactivatedBy;
        return Result.Success();
    }

    /// <summary>
    /// Dissolves the committee permanently with a documented reason.
    /// </summary>
    public Result Dissolve(string dissolvedBy, string reason)
    {
        if (Status == CommitteeStatus.Dissolved)
            return Result.Failure("Committee is already dissolved.");

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("A reason is required for dissolving a committee.");

        Status = CommitteeStatus.Dissolved;
        StatusChangeReason = reason;
        StatusChangedBy = dissolvedBy;
        StatusChangedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = dissolvedBy;

        // Deactivate all active members
        foreach (var member in _members.Where(m => m.IsActive))
        {
            member.Deactivate(dissolvedBy, "Committee dissolved.");
        }

        return Result.Success();
    }

    /// <summary>
    /// Extends the committee's term to a new end date.
    /// </summary>
    public Result ExtendTerm(DateTime newEndDate, string extendedBy)
    {
        if (Status != CommitteeStatus.Active && Status != CommitteeStatus.Expired)
            return Result.Failure("Only active or expired committees can have their term extended.");

        if (newEndDate <= EndDate)
            return Result.Failure("New end date must be after the current end date.");

        EndDate = newEndDate;
        if (Status == CommitteeStatus.Expired)
            Status = CommitteeStatus.Active;

        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = extendedBy;
        return Result.Success();
    }

    /// <summary>
    /// Updates basic committee information.
    /// </summary>
    public Result UpdateInfo(
        string nameAr,
        string nameEn,
        string? description,
        string updatedBy)
    {
        if (Status == CommitteeStatus.Dissolved)
            return Result.Failure("Cannot update a dissolved committee.");

        NameAr = nameAr;
        NameEn = nameEn;
        Description = description;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = updatedBy;
        return Result.Success();
    }

    // ═════════════════════════════════════════════════════════════
    //  Query Helpers
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the count of active members.
    /// </summary>
    public int ActiveMemberCount => _members.Count(m => m.IsActive);

    /// <summary>
    /// Checks if the committee's term has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > EndDate && Status == CommitteeStatus.Active;
}
