namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the status of a technical evaluation for a competition.
/// Follows the PRD Section 9 lifecycle.
/// </summary>
public enum TechnicalEvaluationStatus
{
    /// <summary>Evaluation created but not yet started.</summary>
    Pending = 0,

    /// <summary>Evaluation is in progress — committee members are scoring.</summary>
    InProgress = 1,

    /// <summary>All committee members have submitted their scores.</summary>
    AllScoresSubmitted = 2,

    /// <summary>Report generated and pending committee chair approval.</summary>
    PendingApproval = 3,

    /// <summary>Report approved by committee chair — ready for financial phase.</summary>
    Approved = 4,

    /// <summary>Report rejected by committee chair — requires re-evaluation.</summary>
    Rejected = 5
}
