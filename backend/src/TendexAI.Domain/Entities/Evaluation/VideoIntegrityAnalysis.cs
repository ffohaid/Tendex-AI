using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Represents a video integrity analysis record.
/// Stores the results of AI-driven authenticity checks on recorded scenes,
/// including tamper detection and identity verification.
/// This entity is decoupled from the core evaluation logic to ensure
/// no negative impact on the existing scoring pipeline.
/// </summary>
public sealed class VideoIntegrityAnalysis : BaseEntity<Guid>
{
    private readonly List<VideoAnalysisFlag> _flags = [];

    private VideoIntegrityAnalysis() { } // EF Core parameterless constructor

    public VideoIntegrityAnalysis(
        Guid tenantId,
        Guid competitionId,
        Guid? supplierOfferId,
        string videoFileReference,
        string expectedUserId,
        string? videoFileName = null,
        long? videoFileSizeBytes = null,
        TimeSpan? videoDuration = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        CompetitionId = competitionId;
        SupplierOfferId = supplierOfferId;
        VideoFileReference = videoFileReference;
        VideoFileName = videoFileName;
        VideoFileSizeBytes = videoFileSizeBytes;
        VideoDuration = videoDuration;
        ExpectedUserId = expectedUserId;
        Status = VideoAnalysisStatus.Pending;
        TamperResult = TamperDetectionResult.NotAnalyzed;
        IdentityResult = IdentityVerificationResult.NotVerified;
        OverallConfidenceScore = 0m;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Foreign key to the owning tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Foreign key to the competition this analysis belongs to.</summary>
    public Guid CompetitionId { get; private set; }

    /// <summary>Optional foreign key to a specific supplier offer.</summary>
    public Guid? SupplierOfferId { get; private set; }

    /// <summary>Reference path or key to the video file in storage (MinIO).</summary>
    public string VideoFileReference { get; private set; } = null!;

    /// <summary>Original file name of the uploaded video.</summary>
    public string? VideoFileName { get; private set; }

    /// <summary>Video file size in bytes.</summary>
    public long? VideoFileSizeBytes { get; private set; }

    /// <summary>Duration of the video recording.</summary>
    public TimeSpan? VideoDuration { get; private set; }

    /// <summary>User ID of the person expected to appear in the video.</summary>
    public string ExpectedUserId { get; private set; } = null!;

    /// <summary>Current status of the analysis.</summary>
    public VideoAnalysisStatus Status { get; private set; }

    /// <summary>Result of tamper detection analysis.</summary>
    public TamperDetectionResult TamperResult { get; private set; }

    /// <summary>Confidence score for tamper detection (0.0 to 1.0).</summary>
    public decimal TamperConfidenceScore { get; private set; }

    /// <summary>Result of identity verification.</summary>
    public IdentityVerificationResult IdentityResult { get; private set; }

    /// <summary>Confidence score for identity verification (0.0 to 1.0).</summary>
    public decimal IdentityConfidenceScore { get; private set; }

    /// <summary>Overall confidence score combining all checks (0.0 to 1.0).</summary>
    public decimal OverallConfidenceScore { get; private set; }

    /// <summary>AI provider used for the analysis.</summary>
    public AiProvider? AiProviderUsed { get; private set; }

    /// <summary>AI model name used for the analysis.</summary>
    public string? AiModelUsed { get; private set; }

    /// <summary>Raw AI response JSON for audit trail purposes.</summary>
    public string? RawAiResponse { get; private set; }

    /// <summary>Human-readable summary of the analysis results (in Arabic).</summary>
    public string? AnalysisSummary { get; private set; }

    /// <summary>Error message if analysis failed.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Timestamp when analysis started processing.</summary>
    public DateTime? AnalysisStartedAt { get; private set; }

    /// <summary>Timestamp when analysis completed.</summary>
    public DateTime? AnalysisCompletedAt { get; private set; }

    /// <summary>User ID of the reviewer if manual review was performed.</summary>
    public string? ReviewedByUserId { get; private set; }

    /// <summary>Timestamp of manual review.</summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>Reviewer's notes or override reason.</summary>
    public string? ReviewNotes { get; private set; }

    /// <summary>Collection of specific flags raised during analysis.</summary>
    public IReadOnlyCollection<VideoAnalysisFlag> Flags => _flags.AsReadOnly();

    // ----- Domain Methods -----

    /// <summary>
    /// Marks the analysis as in progress.
    /// </summary>
    public void StartAnalysis()
    {
        if (Status != VideoAnalysisStatus.Pending)
            throw new InvalidOperationException("Analysis can only be started from Pending status.");

        Status = VideoAnalysisStatus.InProgress;
        AnalysisStartedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records the tamper detection result.
    /// </summary>
    public void SetTamperDetectionResult(
        TamperDetectionResult result,
        decimal confidenceScore)
    {
        if (confidenceScore < 0m || confidenceScore > 1m)
            throw new ArgumentOutOfRangeException(nameof(confidenceScore), "Confidence score must be between 0.0 and 1.0.");

        TamperResult = result;
        TamperConfidenceScore = confidenceScore;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records the identity verification result.
    /// </summary>
    public void SetIdentityVerificationResult(
        IdentityVerificationResult result,
        decimal confidenceScore)
    {
        if (confidenceScore < 0m || confidenceScore > 1m)
            throw new ArgumentOutOfRangeException(nameof(confidenceScore), "Confidence score must be between 0.0 and 1.0.");

        IdentityResult = result;
        IdentityConfidenceScore = confidenceScore;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes the analysis with the final results.
    /// </summary>
    public void CompleteAnalysis(
        decimal overallConfidenceScore,
        AiProvider aiProvider,
        string aiModel,
        string? rawAiResponse,
        string? analysisSummary)
    {
        if (overallConfidenceScore < 0m || overallConfidenceScore > 1m)
            throw new ArgumentOutOfRangeException(nameof(overallConfidenceScore));

        OverallConfidenceScore = overallConfidenceScore;
        AiProviderUsed = aiProvider;
        AiModelUsed = aiModel;
        RawAiResponse = rawAiResponse;
        AnalysisSummary = analysisSummary;
        AnalysisCompletedAt = DateTime.UtcNow;

        // Determine final status based on results
        bool tamperPassed = TamperResult == TamperDetectionResult.Genuine;
        bool identityPassed = IdentityResult == IdentityVerificationResult.Confirmed;

        if (tamperPassed && identityPassed && overallConfidenceScore >= 0.7m)
            Status = VideoAnalysisStatus.Passed;
        else if (TamperResult == TamperDetectionResult.Inconclusive ||
                 IdentityResult == IdentityVerificationResult.Inconclusive)
            Status = VideoAnalysisStatus.ManualReviewRequired;
        else
            Status = VideoAnalysisStatus.Failed;

        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the analysis as failed due to an error.
    /// </summary>
    public void FailAnalysis(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        Status = VideoAnalysisStatus.Error;
        ErrorMessage = errorMessage;
        AnalysisCompletedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a manual review decision.
    /// </summary>
    public void RecordManualReview(
        string reviewerUserId,
        VideoAnalysisStatus overrideStatus,
        string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewerUserId);

        if (overrideStatus != VideoAnalysisStatus.Passed &&
            overrideStatus != VideoAnalysisStatus.Failed)
            throw new ArgumentException("Manual review can only set status to Passed or Failed.");

        ReviewedByUserId = reviewerUserId;
        ReviewedAt = DateTime.UtcNow;
        ReviewNotes = notes;
        Status = overrideStatus;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a specific flag raised during analysis.
    /// </summary>
    public void AddFlag(string flagCode, string description, string severity, decimal? confidence = null)
    {
        var flag = new VideoAnalysisFlag(Id, flagCode, description, severity, confidence);
        _flags.Add(flag);
        LastModifiedAt = DateTime.UtcNow;
    }
}
