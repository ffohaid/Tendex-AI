using TendexAI.Domain.Enums;
using TendexAI.Domain.Services;

namespace TendexAI.Application.Features.TechnicalEvaluation.Dtos;

// ═══════════════════════════════════════════════════════════════
//  Technical Evaluation DTOs
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Detailed view of a technical evaluation session.
/// </summary>
public sealed record TechnicalEvaluationDetailDto(
    Guid Id,
    Guid CompetitionId,
    Guid? CommitteeId,
    TechnicalEvaluationStatus Status,
    decimal MinimumPassingScore,
    bool IsBlindEvaluationActive,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? ApprovedAt,
    string? ApprovedBy,
    string? RejectionReason,
    DateTime CreatedAt);

/// <summary>
/// Summary view of an offer during blind evaluation.
/// Supplier identity is hidden when blind evaluation is active.
/// </summary>
public sealed record BlindOfferSummaryDto(
    Guid OfferId,
    string BlindCode,
    string? SupplierName,  // null when blind evaluation is active
    decimal? TechnicalTotalScore,
    OfferTechnicalResult TechnicalResult,
    bool IsFinancialEnvelopeOpen);

/// <summary>
/// Score entry from a committee member.
/// </summary>
public sealed record TechnicalScoreDto(
    Guid Id,
    Guid SupplierOfferId,
    string OfferBlindCode,
    Guid EvaluationCriterionId,
    string CriterionNameAr,
    string CriterionNameEn,
    string EvaluatorUserId,
    decimal Score,
    decimal MaxScore,
    decimal ScorePercentage,
    string? Notes,
    DateTime CreatedAt);

/// <summary>
/// AI-generated score suggestion.
/// </summary>
public sealed record AiTechnicalScoreDto(
    Guid Id,
    Guid SupplierOfferId,
    string OfferBlindCode,
    Guid EvaluationCriterionId,
    string CriterionNameAr,
    string CriterionNameEn,
    decimal SuggestedScore,
    decimal MaxScore,
    decimal ScorePercentage,
    string Justification,
    string? ReferenceCitations);

/// <summary>
/// Variance alert for a criterion where scores diverge significantly.
/// </summary>
public sealed record VarianceAlertDto(
    Guid CriterionId,
    string CriterionNameAr,
    string CriterionNameEn,
    Guid OfferId,
    string OfferBlindCode,
    bool HasEvaluatorVariance,
    bool HasHumanAiVariance,
    decimal? EvaluatorSpread,
    decimal? HumanAiDifference);

/// <summary>
/// Heatmap cell for the comparison matrix.
/// </summary>
public sealed record HeatmapCellDto(
    string OfferBlindCode,
    Guid OfferId,
    Guid CriterionId,
    string CriterionNameAr,
    string CriterionNameEn,
    decimal AverageScorePercentage,
    HeatmapColor Color);

/// <summary>
/// Complete heatmap data for all offers and criteria.
/// </summary>
public sealed record TechnicalHeatmapDto(
    Guid CompetitionId,
    Guid EvaluationId,
    IReadOnlyList<string> OfferBlindCodes,
    IReadOnlyList<CriterionHeaderDto> Criteria,
    IReadOnlyList<HeatmapCellDto> Cells);

/// <summary>
/// Criterion header for heatmap display.
/// </summary>
public sealed record CriterionHeaderDto(
    Guid Id,
    string NameAr,
    string NameEn,
    decimal WeightPercentage,
    decimal? MinimumPassingScore);

/// <summary>
/// Summary of offer evaluation results after scoring is complete.
/// </summary>
public sealed record OfferEvaluationResultDto(
    Guid OfferId,
    string BlindCode,
    string? SupplierName,
    decimal WeightedTotalScore,
    OfferTechnicalResult Result,
    int Rank,
    IReadOnlyList<CriterionScoreSummaryDto> CriterionScores);

/// <summary>
/// Summary of scores for a single criterion across all evaluators.
/// </summary>
public sealed record CriterionScoreSummaryDto(
    Guid CriterionId,
    string CriterionNameAr,
    string CriterionNameEn,
    decimal WeightPercentage,
    decimal AverageScore,
    decimal AveragePercentage,
    decimal? AiSuggestedScore,
    decimal? AiPercentage,
    bool HasVariance,
    IReadOnlyList<EvaluatorScoreDto> EvaluatorScores);

/// <summary>
/// Individual evaluator's score for a criterion.
/// </summary>
public sealed record EvaluatorScoreDto(
    string EvaluatorUserId,
    decimal Score,
    decimal MaxScore,
    decimal Percentage,
    string? Notes);
