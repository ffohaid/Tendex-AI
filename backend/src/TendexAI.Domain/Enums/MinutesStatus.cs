namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the status of an evaluation minutes document.
/// Per PRD Section 11.
/// </summary>
public enum MinutesStatus
{
    /// <summary>Minutes draft created.</summary>
    Draft = 0,

    /// <summary>Minutes submitted for approval.</summary>
    PendingApproval = 1,

    /// <summary>Minutes approved and finalized.</summary>
    Approved = 2,

    /// <summary>Minutes rejected — requires revision.</summary>
    Rejected = 3
}
