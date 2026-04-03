namespace TendexAI.Domain.Enums;

/// <summary>
/// Defines the scope of authority for a committee.
/// Determines how the committee's jurisdiction is applied across competitions and phases.
/// </summary>
public enum CommitteeScopeType
{
    /// <summary>
    /// شاملة — Committee has authority over all phases of all competitions.
    /// No phase or competition restrictions apply.
    /// </summary>
    Comprehensive = 1,

    /// <summary>
    /// مراحل محددة لجميع المنافسات — Committee is active only during specific phases,
    /// but applies to all competitions within the tenant.
    /// </summary>
    SpecificPhasesAllCompetitions = 2,

    /// <summary>
    /// مراحل محددة لمنافسات محددة — Committee is active only during specific phases
    /// and only for explicitly linked competitions.
    /// </summary>
    SpecificPhasesSpecificCompetitions = 3
}
