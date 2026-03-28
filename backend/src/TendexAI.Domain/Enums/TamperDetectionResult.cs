namespace TendexAI.Domain.Enums;

/// <summary>
/// Result of tamper detection analysis on a video recording.
/// Indicates whether the recording appears genuine or has signs of manipulation.
/// </summary>
public enum TamperDetectionResult
{
    /// <summary>Not yet analyzed.</summary>
    NotAnalyzed = 0,

    /// <summary>Recording appears genuine with high confidence.</summary>
    Genuine = 1,

    /// <summary>Possible re-recording from another screen detected (e.g., phone, TV, monitor).</summary>
    ScreenRecordingSuspected = 2,

    /// <summary>Video editing or splicing artifacts detected.</summary>
    EditingDetected = 3,

    /// <summary>Deepfake or synthetic media indicators found.</summary>
    DeepfakeSuspected = 4,

    /// <summary>Metadata inconsistencies detected (e.g., timestamps, codec anomalies).</summary>
    MetadataInconsistency = 5,

    /// <summary>Analysis was inconclusive; manual review recommended.</summary>
    Inconclusive = 6
}
