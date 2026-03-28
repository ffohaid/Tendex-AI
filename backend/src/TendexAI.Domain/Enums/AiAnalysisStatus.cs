namespace TendexAI.Domain.Enums;

/// <summary>
/// Status of an AI-generated offer analysis.
/// </summary>
public enum AiAnalysisStatus
{
    /// <summary>Analysis is currently being processed by AI.</summary>
    Processing = 1,

    /// <summary>Analysis completed successfully.</summary>
    Completed = 2,

    /// <summary>Analysis has been reviewed by a human committee member.</summary>
    Reviewed = 3,

    /// <summary>Analysis failed due to an AI processing error.</summary>
    Failed = 4
}
