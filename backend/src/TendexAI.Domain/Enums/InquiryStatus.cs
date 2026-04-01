namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a supplier inquiry.
/// Follows the workflow: New → InProgress → PendingApproval → Approved/Rejected.
/// </summary>
public enum InquiryStatus
{
    /// <summary>Newly created inquiry, not yet assigned or answered.</summary>
    New = 0,

    /// <summary>Inquiry has been assigned and is being worked on.</summary>
    InProgress = 1,

    /// <summary>Answer has been drafted and is awaiting committee approval.</summary>
    PendingApproval = 2,

    /// <summary>Answer has been approved by the committee chair.</summary>
    Approved = 3,

    /// <summary>Answer has been rejected and needs revision.</summary>
    Rejected = 4,

    /// <summary>Inquiry has been closed (e.g., duplicate, withdrawn).</summary>
    Closed = 5
}
