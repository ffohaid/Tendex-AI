using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.VideoAnalysis.Dtos;

/// <summary>
/// Data transfer object for video integrity analysis results.
/// </summary>
public sealed record VideoIntegrityAnalysisDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid CompetitionId { get; init; }
    public Guid? SupplierOfferId { get; init; }
    public string VideoFileReference { get; init; } = null!;
    public string? VideoFileName { get; init; }
    public long? VideoFileSizeBytes { get; init; }
    public TimeSpan? VideoDuration { get; init; }
    public string ExpectedUserId { get; init; } = null!;
    public VideoAnalysisStatus Status { get; init; }
    public string StatusDisplayName { get; init; } = null!;
    public TamperDetectionResult TamperResult { get; init; }
    public string TamperResultDisplayName { get; init; } = null!;
    public decimal TamperConfidenceScore { get; init; }
    public IdentityVerificationResult IdentityResult { get; init; }
    public string IdentityResultDisplayName { get; init; } = null!;
    public decimal IdentityConfidenceScore { get; init; }
    public decimal OverallConfidenceScore { get; init; }
    public string? AiProviderUsed { get; init; }
    public string? AiModelUsed { get; init; }
    public string? AnalysisSummary { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? AnalysisStartedAt { get; init; }
    public DateTime? AnalysisCompletedAt { get; init; }
    public string? ReviewedByUserId { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ReviewNotes { get; init; }
    public IReadOnlyList<VideoAnalysisFlagDto> Flags { get; init; } = [];
}

/// <summary>
/// Data transfer object for a specific flag raised during analysis.
/// </summary>
public sealed record VideoAnalysisFlagDto
{
    public Guid Id { get; init; }
    public string FlagCode { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string Severity { get; init; } = null!;
    public decimal? Confidence { get; init; }
}
