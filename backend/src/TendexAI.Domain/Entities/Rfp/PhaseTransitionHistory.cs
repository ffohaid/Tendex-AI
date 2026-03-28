using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Immutable audit record for every competition phase transition.
/// Part of the immutable audit trail required by PRD Section 20.
/// Each record captures who initiated the transition, when, and why.
/// </summary>
public sealed class PhaseTransitionHistory : BaseEntity<Guid>
{
    private PhaseTransitionHistory() { } // EF Core constructor

    public static PhaseTransitionHistory Create(
        Guid competitionId,
        Guid tenantId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        CompetitionPhase fromPhase,
        CompetitionPhase toPhase,
        string transitionedByUserId,
        string? reason = null,
        string? metadata = null)
    {
        return new PhaseTransitionHistory
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            TenantId = tenantId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            FromPhase = fromPhase,
            ToPhase = toPhase,
            TransitionedByUserId = transitionedByUserId,
            Reason = reason,
            Metadata = metadata,
            TransitionedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = transitionedByUserId
        };
    }

    /// <summary>The competition this transition belongs to.</summary>
    public Guid CompetitionId { get; private set; }

    /// <summary>The tenant this transition belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The status before the transition.</summary>
    public CompetitionStatus FromStatus { get; private set; }

    /// <summary>The status after the transition.</summary>
    public CompetitionStatus ToStatus { get; private set; }

    /// <summary>The phase before the transition.</summary>
    public CompetitionPhase FromPhase { get; private set; }

    /// <summary>The phase after the transition.</summary>
    public CompetitionPhase ToPhase { get; private set; }

    /// <summary>User who initiated the transition.</summary>
    public string TransitionedByUserId { get; private set; } = default!;

    /// <summary>Optional reason for the transition (required for rejections/cancellations).</summary>
    public string? Reason { get; private set; }

    /// <summary>Optional JSON metadata about the transition (e.g., prerequisite check results).</summary>
    public string? Metadata { get; private set; }

    /// <summary>Exact timestamp of the transition.</summary>
    public DateTime TransitionedAt { get; private set; }
}
