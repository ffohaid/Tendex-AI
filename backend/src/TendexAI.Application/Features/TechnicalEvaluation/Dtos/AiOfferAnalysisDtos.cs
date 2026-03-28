using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.TechnicalEvaluation.Dtos;

// ═══════════════════════════════════════════════════════════════
//  AI Offer Analysis DTOs
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Comprehensive AI analysis of a supplier's technical offer.
/// All outputs are labeled as "مسودة مقترحة من الذكاء الاصطناعي".
/// </summary>
public sealed record AiOfferAnalysisDto(
    Guid Id,
    Guid TechnicalEvaluationId,
    Guid SupplierOfferId,
    string BlindCode,
    string ExecutiveSummary,
    string StrengthsAnalysis,
    string WeaknessesAnalysis,
    string RisksAnalysis,
    string ComplianceAssessment,
    string OverallRecommendation,
    decimal OverallComplianceScore,
    AiAnalysisStatus Status,
    string AiModelUsed,
    string AiProviderUsed,
    long AnalysisLatencyMs,
    bool IsHumanReviewed,
    string? ReviewedBy,
    DateTime? ReviewedAt,
    string? ReviewNotes,
    DateTime CreatedAt,
    IReadOnlyList<AiCriterionAnalysisDto> CriterionAnalyses);

/// <summary>
/// AI analysis for a single evaluation criterion.
/// </summary>
public sealed record AiCriterionAnalysisDto(
    Guid Id,
    Guid EvaluationCriterionId,
    string CriterionNameAr,
    decimal SuggestedScore,
    decimal MaxScore,
    decimal ScorePercentage,
    string DetailedJustification,
    string OfferCitations,
    string? BookletRequirementReference,
    string ComplianceNotes,
    AiCriterionComplianceLevel ComplianceLevel);

/// <summary>
/// Summary of AI analysis for all offers in an evaluation.
/// </summary>
public sealed record AiAnalysisSummaryDto(
    Guid TechnicalEvaluationId,
    Guid CompetitionId,
    int TotalOffers,
    int CompletedAnalyses,
    int FailedAnalyses,
    int PendingReviews,
    IReadOnlyList<AiOfferAnalysisSummaryItemDto> OfferSummaries);

/// <summary>
/// Brief summary of a single offer's AI analysis.
/// </summary>
public sealed record AiOfferAnalysisSummaryItemDto(
    Guid SupplierOfferId,
    string BlindCode,
    decimal OverallComplianceScore,
    AiAnalysisStatus Status,
    bool IsHumanReviewed,
    int CriteriaAnalyzed,
    int FullyCompliantCount,
    int PartiallyCompliantCount,
    int NonCompliantCount,
    int RequiresReviewCount);

/// <summary>
/// Comparison matrix showing AI analysis across all offers and criteria.
/// </summary>
public sealed record AiComparisonMatrixDto(
    Guid TechnicalEvaluationId,
    IReadOnlyList<string> OfferBlindCodes,
    IReadOnlyList<CriterionHeaderDto> Criteria,
    IReadOnlyList<AiComparisonCellDto> Cells);

/// <summary>
/// A single cell in the AI comparison matrix.
/// </summary>
public sealed record AiComparisonCellDto(
    string BlindCode,
    Guid CriterionId,
    string CriterionNameAr,
    decimal SuggestedScore,
    decimal MaxScore,
    decimal ScorePercentage,
    AiCriterionComplianceLevel ComplianceLevel,
    string JustificationSummary);

/// <summary>
/// Request to trigger AI analysis for all offers in an evaluation.
/// </summary>
public sealed record TriggerAiAnalysisRequestDto(
    Guid EvaluationId);

/// <summary>
/// Request to mark an AI analysis as reviewed by a human.
/// </summary>
public sealed record ReviewAiAnalysisRequestDto(
    Guid AnalysisId,
    string? ReviewNotes);
