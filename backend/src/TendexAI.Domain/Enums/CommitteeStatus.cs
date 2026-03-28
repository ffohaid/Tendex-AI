namespace TendexAI.Domain.Enums;

/// <summary>
/// Defines the lifecycle status of a committee.
/// Maps to PRD Section 4.3 — committee management states.
/// </summary>
public enum CommitteeStatus
{
    /// <summary>اللجنة نشطة وتعمل حالياً — Committee is active and operational.</summary>
    Active = 1,

    /// <summary>اللجنة معلقة مؤقتاً — Committee is temporarily suspended.</summary>
    Suspended = 2,

    /// <summary>اللجنة منحلة — Committee has been dissolved.</summary>
    Dissolved = 3,

    /// <summary>انتهت فترة عمل اللجنة — Committee term has expired.</summary>
    Expired = 4
}
