namespace TendexAI.Domain.Enums;

/// <summary>
/// Defines the role a user holds within a specific committee instance.
/// This is distinct from <see cref="CommitteeRole"/> which maps to the 4D permission matrix.
/// Maps to PRD Section 4.3 — member roles within a committee.
/// </summary>
public enum CommitteeMemberRole
{
    /// <summary>رئيس اللجنة — Committee Chair / President.</summary>
    Chair = 1,

    /// <summary>عضو اللجنة — Committee Member.</summary>
    Member = 2,

    /// <summary>سكرتير اللجنة — Committee Secretary.</summary>
    Secretary = 3
}
