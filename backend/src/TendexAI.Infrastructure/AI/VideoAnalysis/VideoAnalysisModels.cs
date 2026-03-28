using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.AI.VideoAnalysis;

/// <summary>
/// Internal model representing the structured AI response for video integrity analysis.
/// The AI provider returns JSON that is deserialized into this model.
/// </summary>
internal sealed class AiVideoAnalysisResponse
{
    /// <summary>Whether the video appears to be a genuine, original recording.</summary>
    public bool IsGenuineRecording { get; set; }

    /// <summary>Confidence score for the genuineness assessment (0.0 to 1.0).</summary>
    public decimal GenuinenessConfidence { get; set; }

    /// <summary>Tamper detection details.</summary>
    public TamperDetectionDetails TamperDetection { get; set; } = new();

    /// <summary>Identity verification details.</summary>
    public IdentityVerificationDetails IdentityVerification { get; set; } = new();

    /// <summary>List of specific flags or concerns identified.</summary>
    public List<AnalysisFlagDetail> Flags { get; set; } = [];

    /// <summary>Human-readable summary of the analysis (in Arabic).</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Overall confidence score (0.0 to 1.0).</summary>
    public decimal OverallConfidence { get; set; }
}

/// <summary>
/// Details of tamper detection analysis.
/// </summary>
internal sealed class TamperDetectionDetails
{
    /// <summary>Whether screen recording artifacts (moiré patterns, refresh lines, bezels) were detected.</summary>
    public bool ScreenRecordingDetected { get; set; }

    /// <summary>Confidence for screen recording detection (0.0 to 1.0).</summary>
    public decimal ScreenRecordingConfidence { get; set; }

    /// <summary>Whether video editing or splicing was detected.</summary>
    public bool EditingDetected { get; set; }

    /// <summary>Confidence for editing detection (0.0 to 1.0).</summary>
    public decimal EditingConfidence { get; set; }

    /// <summary>Whether deepfake indicators were found.</summary>
    public bool DeepfakeIndicators { get; set; }

    /// <summary>Confidence for deepfake detection (0.0 to 1.0).</summary>
    public decimal DeepfakeConfidence { get; set; }

    /// <summary>Whether metadata inconsistencies were found.</summary>
    public bool MetadataInconsistencies { get; set; }

    /// <summary>Specific observations about the recording environment.</summary>
    public string EnvironmentNotes { get; set; } = string.Empty;

    /// <summary>Overall tamper confidence score (0.0 to 1.0).</summary>
    public decimal OverallTamperConfidence { get; set; }
}

/// <summary>
/// Details of identity verification analysis.
/// </summary>
internal sealed class IdentityVerificationDetails
{
    /// <summary>Whether a face was detected in the video.</summary>
    public bool FaceDetected { get; set; }

    /// <summary>Number of distinct faces detected.</summary>
    public int FaceCount { get; set; }

    /// <summary>Whether the detected face matches the expected person.</summary>
    public bool IdentityMatch { get; set; }

    /// <summary>Confidence score for the identity match (0.0 to 1.0).</summary>
    public decimal MatchConfidence { get; set; }

    /// <summary>Quality assessment of the face capture.</summary>
    public string QualityAssessment { get; set; } = string.Empty;

    /// <summary>Whether the face quality is sufficient for reliable verification.</summary>
    public bool SufficientQuality { get; set; }
}

/// <summary>
/// A specific flag or concern identified during analysis.
/// </summary>
internal sealed class AnalysisFlagDetail
{
    /// <summary>Machine-readable flag code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable description (in Arabic).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Severity: "Low", "Medium", "High", "Critical".</summary>
    public string Severity { get; set; } = "Low";

    /// <summary>Confidence for this specific flag (0.0 to 1.0).</summary>
    public decimal Confidence { get; set; }
}
