namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the status of an award recommendation for a competition.
/// Per PRD Section 11 — award lifecycle.
/// </summary>
public enum AwardStatus
{
    /// <summary>Award recommendation generated but not yet submitted.</summary>
    Draft = 0,

    /// <summary>Award recommendation submitted for authority holder approval.</summary>
    PendingApproval = 1,

    /// <summary>Award approved by authority holder.</summary>
    Approved = 2,

    /// <summary>Award rejected by authority holder — requires review.</summary>
    Rejected = 3
}
