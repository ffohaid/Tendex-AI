using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Committees;

/// <summary>
/// Represents the many-to-many relationship between a committee and competitions.
/// Used when <see cref="Enums.CommitteeScopeType.SpecificPhasesSpecificCompetitions"/>
/// is selected, linking the committee to one or more specific competitions.
/// </summary>
public sealed class CommitteeCompetition : BaseEntity<Guid>
{
    /// <summary>Required for EF Core.</summary>
    private CommitteeCompetition() { }

    /// <summary>
    /// Creates a new committee-competition link.
    /// </summary>
    public CommitteeCompetition(Guid committeeId, Guid competitionId, string assignedBy)
    {
        Id = Guid.NewGuid();
        CommitteeId = committeeId;
        CompetitionId = competitionId;
        AssignedAt = DateTime.UtcNow;
        AssignedBy = assignedBy;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = assignedBy;
    }

    /// <summary>The committee this link belongs to.</summary>
    public Guid CommitteeId { get; private set; }

    /// <summary>The competition linked to the committee.</summary>
    public Guid CompetitionId { get; private set; }

    /// <summary>When this competition was linked to the committee.</summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>Who linked this competition to the committee.</summary>
    public string AssignedBy { get; private set; } = default!;

    /// <summary>Navigation property — the parent committee.</summary>
    public Committee Committee { get; private set; } = default!;
}
