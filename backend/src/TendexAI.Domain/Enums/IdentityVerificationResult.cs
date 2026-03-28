namespace TendexAI.Domain.Enums;

/// <summary>
/// Result of identity verification for the person appearing in a video recording.
/// </summary>
public enum IdentityVerificationResult
{
    /// <summary>Not yet verified.</summary>
    NotVerified = 0,

    /// <summary>Identity confirmed — the person matches the expected user.</summary>
    Confirmed = 1,

    /// <summary>Identity mismatch — the person does not match the expected user.</summary>
    Mismatch = 2,

    /// <summary>No face detected in the video recording.</summary>
    NoFaceDetected = 3,

    /// <summary>Multiple faces detected; cannot determine primary subject.</summary>
    MultipleFacesDetected = 4,

    /// <summary>Face quality too low for reliable verification (blur, occlusion, lighting).</summary>
    LowQuality = 5,

    /// <summary>Verification was inconclusive; manual review recommended.</summary>
    Inconclusive = 6
}
