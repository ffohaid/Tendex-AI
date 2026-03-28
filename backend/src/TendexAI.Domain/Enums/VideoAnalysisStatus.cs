namespace TendexAI.Domain.Enums;

/// <summary>
/// Status of a video integrity analysis request.
/// </summary>
public enum VideoAnalysisStatus
{
    /// <summary>Analysis request has been queued but not yet started.</summary>
    Pending = 0,

    /// <summary>Analysis is currently in progress.</summary>
    InProgress = 1,

    /// <summary>Analysis completed successfully with all checks passed.</summary>
    Passed = 2,

    /// <summary>Analysis completed but one or more checks failed.</summary>
    Failed = 3,

    /// <summary>Analysis could not be completed due to an error.</summary>
    Error = 4,

    /// <summary>Analysis requires manual review by a human operator.</summary>
    ManualReviewRequired = 5
}
