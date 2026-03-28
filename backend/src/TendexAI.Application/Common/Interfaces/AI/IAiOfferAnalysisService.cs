using TendexAI.Domain.Common;

namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Service interface for AI-powered technical offer analysis.
/// Analyzes supplier offers against the terms and specifications booklet (كراسة الشروط والمواصفات)
/// and provides structured recommendations for examination committees.
/// 
/// Per RAG Guidelines:
/// - Uses RAG pipeline to ground analysis in actual document content
/// - Enforces blind evaluation (no supplier identity exposure)
/// - All outputs in Arabic
/// - AI acts as assistant (Copilot), not decision maker
/// </summary>
public interface IAiOfferAnalysisService
{
    /// <summary>
    /// Analyzes a single supplier offer against the competition's booklet and criteria.
    /// Returns a structured analysis result including per-criterion scoring.
    /// </summary>
    /// <param name="request">The analysis request containing offer and booklet context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Structured analysis result or failure.</returns>
    Task<Result<AiOfferAnalysisResult>> AnalyzeOfferAsync(
        AiOfferAnalysisRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for AI offer analysis.
/// </summary>
public sealed record AiOfferAnalysisRequest
{
    /// <summary>Tenant identifier for AI configuration resolution.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Competition identifier.</summary>
    public required Guid CompetitionId { get; init; }

    /// <summary>Blind code for the offer (no supplier identity).</summary>
    public required string BlindCode { get; init; }

    /// <summary>
    /// The technical offer content (extracted text from offer documents).
    /// </summary>
    public required string OfferContent { get; init; }

    /// <summary>
    /// The terms and specifications booklet content (extracted text from RFP sections).
    /// </summary>
    public required string BookletContent { get; init; }

    /// <summary>
    /// Evaluation criteria with their weights and descriptions.
    /// </summary>
    public required IReadOnlyList<CriterionForAnalysis> Criteria { get; init; }

    /// <summary>
    /// Minimum passing score for the technical evaluation.
    /// </summary>
    public required decimal MinimumPassingScore { get; init; }

    /// <summary>
    /// Competition project name in Arabic for context.
    /// </summary>
    public required string ProjectNameAr { get; init; }
}

/// <summary>
/// Criterion information passed to the AI for analysis.
/// </summary>
public sealed record CriterionForAnalysis
{
    public required Guid Id { get; init; }
    public required string NameAr { get; init; }
    public required string NameEn { get; init; }
    public required string? DescriptionAr { get; init; }
    public required decimal WeightPercentage { get; init; }
    public required decimal MaxScore { get; init; }
    public required decimal? MinimumPassingScore { get; init; }
}

/// <summary>
/// Structured result from AI offer analysis.
/// </summary>
public sealed record AiOfferAnalysisResult
{
    /// <summary>Executive summary of the analysis.</summary>
    public required string ExecutiveSummary { get; init; }

    /// <summary>Strengths identified in the offer.</summary>
    public required string StrengthsAnalysis { get; init; }

    /// <summary>Weaknesses identified in the offer.</summary>
    public required string WeaknessesAnalysis { get; init; }

    /// <summary>Risks identified in the offer.</summary>
    public required string RisksAnalysis { get; init; }

    /// <summary>Compliance assessment against the booklet.</summary>
    public required string ComplianceAssessment { get; init; }

    /// <summary>Overall recommendation for the committee.</summary>
    public required string OverallRecommendation { get; init; }

    /// <summary>Overall compliance score (0-100).</summary>
    public required decimal OverallComplianceScore { get; init; }

    /// <summary>Per-criterion analysis results.</summary>
    public required IReadOnlyList<CriterionAnalysisResult> CriterionResults { get; init; }

    /// <summary>AI model used for the analysis.</summary>
    public required string AiModelUsed { get; init; }

    /// <summary>AI provider used.</summary>
    public required string AiProviderUsed { get; init; }

    /// <summary>Analysis latency in milliseconds.</summary>
    public required long AnalysisLatencyMs { get; init; }
}

/// <summary>
/// Per-criterion analysis result from AI.
/// </summary>
public sealed record CriterionAnalysisResult
{
    /// <summary>Criterion identifier.</summary>
    public required Guid CriterionId { get; init; }

    /// <summary>Arabic name of the criterion.</summary>
    public required string CriterionNameAr { get; init; }

    /// <summary>Suggested score for this criterion.</summary>
    public required decimal SuggestedScore { get; init; }

    /// <summary>Maximum possible score.</summary>
    public required decimal MaxScore { get; init; }

    /// <summary>Detailed justification for the score.</summary>
    public required string DetailedJustification { get; init; }

    /// <summary>Direct citations from the offer document.</summary>
    public required string OfferCitations { get; init; }

    /// <summary>Reference to the booklet requirement.</summary>
    public required string? BookletRequirementReference { get; init; }

    /// <summary>Compliance notes for this criterion.</summary>
    public required string ComplianceNotes { get; init; }

    /// <summary>Compliance level assessment.</summary>
    public required string ComplianceLevel { get; init; }
}
