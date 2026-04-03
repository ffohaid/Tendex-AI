namespace TendexAI.Domain.Enums;

/// <summary>
/// Defines the scope at which a permission rule applies.
/// Used as the first dimension of the flexible permission matrix.
/// </summary>
public enum ResourceScope
{
    /// <summary>
    /// Global scope — applies to tenant-wide resources
    /// (e.g., organization settings, user management, AI configuration).
    /// </summary>
    Global = 0,

    /// <summary>
    /// Competition scope — applies to resources within a specific competition
    /// (e.g., booklet sections, offers, evaluations).
    /// </summary>
    Competition = 1,

    /// <summary>
    /// Committee scope — applies to resources within a specific committee
    /// (e.g., committee minutes, member management).
    /// </summary>
    Committee = 2
}
