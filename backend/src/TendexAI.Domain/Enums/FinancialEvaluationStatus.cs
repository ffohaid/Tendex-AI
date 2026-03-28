namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the status of a financial evaluation for a competition.
/// Follows the PRD Section 10 lifecycle.
/// Financial evaluation can only start after technical evaluation is approved.
/// </summary>
public enum FinancialEvaluationStatus
{
    /// <summary>Financial evaluation created but not yet started.</summary>
    Pending = 0,

    /// <summary>Financial envelopes opened — evaluation is in progress.</summary>
    InProgress = 1,

    /// <summary>All financial scores and verifications are submitted.</summary>
    AllScoresSubmitted = 2,

    /// <summary>Report generated and pending committee chair approval.</summary>
    PendingApproval = 3,

    /// <summary>Report approved by committee chair and financial controller.</summary>
    Approved = 4,

    /// <summary>Report rejected — requires re-evaluation.</summary>
    Rejected = 5
}
