using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Represents a specific flag or issue raised during video integrity analysis.
/// Each flag indicates a particular concern or observation about the video recording.
/// </summary>
public sealed class VideoAnalysisFlag : BaseEntity<Guid>
{
    private VideoAnalysisFlag() { } // EF Core parameterless constructor

    public VideoAnalysisFlag(
        Guid videoIntegrityAnalysisId,
        string flagCode,
        string description,
        string severity,
        decimal? confidence = null)
    {
        Id = Guid.NewGuid();
        VideoIntegrityAnalysisId = videoIntegrityAnalysisId;
        FlagCode = flagCode;
        Description = description;
        Severity = severity;
        Confidence = confidence;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Foreign key to the parent video integrity analysis.</summary>
    public Guid VideoIntegrityAnalysisId { get; private set; }

    /// <summary>Navigation property to the parent analysis.</summary>
    public VideoIntegrityAnalysis VideoIntegrityAnalysis { get; private set; } = null!;

    /// <summary>
    /// Machine-readable flag code (e.g., "SCREEN_RECORDING", "MOIRE_PATTERN",
    /// "FACE_MISMATCH", "METADATA_TAMPER", "LOW_RESOLUTION").
    /// </summary>
    public string FlagCode { get; private set; } = null!;

    /// <summary>Human-readable description of the flag (in Arabic).</summary>
    public string Description { get; private set; } = null!;

    /// <summary>Severity level: "Low", "Medium", "High", "Critical".</summary>
    public string Severity { get; private set; } = null!;

    /// <summary>Confidence score for this specific flag (0.0 to 1.0), if applicable.</summary>
    public decimal? Confidence { get; private set; }
}
