using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Services;

/// <summary>
/// Domain service for calculating technical evaluation scores.
/// Implements weighted scoring, variance detection, and pass/fail determination.
/// Per PRD Sections 9.1-9.4.
/// </summary>
public static class TechnicalScoringService
{
    /// <summary>
    /// Variance threshold (20%) for flagging discrepancies between human and AI scores,
    /// or between committee members. Per PRD Section 9.2.
    /// </summary>
    public const decimal VarianceThreshold = 20m;

    /// <summary>
    /// Calculates the weighted total score for a supplier offer across all criteria.
    /// Uses the average of all committee members' scores per criterion,
    /// weighted by each criterion's weight percentage.
    /// </summary>
    /// <param name="scores">All human scores for this offer.</param>
    /// <param name="criteria">The evaluation criteria with weights.</param>
    /// <returns>Weighted total score (0-100).</returns>
    public static decimal CalculateWeightedTotalScore(
        IReadOnlyList<TechnicalScore> scores,
        IReadOnlyList<EvaluationCriterion> criteria)
    {
        if (scores.Count == 0 || criteria.Count == 0)
            return 0m;

        decimal totalWeightedScore = 0m;
        decimal totalWeight = 0m;

        foreach (var criterion in criteria.Where(c => c.IsActive && c.ParentCriterionId == null))
        {
            var criterionScores = scores
                .Where(s => s.EvaluationCriterionId == criterion.Id)
                .ToList();

            if (criterionScores.Count == 0)
                continue;

            // Average score percentage across all evaluators
            decimal averagePercentage = criterionScores.Average(s => s.GetScorePercentage());

            // Apply criterion weight
            totalWeightedScore += averagePercentage * (criterion.WeightPercentage / 100m);
            totalWeight += criterion.WeightPercentage;
        }

        // Normalize if weights don't sum to 100
        return totalWeight > 0 ? (totalWeightedScore / totalWeight) * 100m : 0m;
    }

    /// <summary>
    /// Determines if a supplier offer passes technical evaluation
    /// based on the minimum passing score.
    /// </summary>
    public static OfferTechnicalResult DetermineResult(
        decimal weightedTotalScore,
        decimal minimumPassingScore)
    {
        return weightedTotalScore >= minimumPassingScore
            ? OfferTechnicalResult.Passed
            : OfferTechnicalResult.Failed;
    }

    /// <summary>
    /// Detects variance between human evaluators' scores for a criterion.
    /// Returns true if the spread exceeds the threshold (20%).
    /// Per PRD Section 9.2.
    /// </summary>
    public static bool HasEvaluatorVariance(IReadOnlyList<TechnicalScore> criterionScores)
    {
        if (criterionScores.Count < 2)
            return false;

        var percentages = criterionScores.Select(s => s.GetScorePercentage()).ToList();
        decimal maxSpread = percentages.Max() - percentages.Min();

        return maxSpread > VarianceThreshold;
    }

    /// <summary>
    /// Detects variance between human average and AI score for a criterion.
    /// Returns true if the difference exceeds the threshold (20%).
    /// Per PRD Section 9.2.
    /// </summary>
    public static bool HasHumanAiVariance(
        IReadOnlyList<TechnicalScore> humanScores,
        AiTechnicalScore? aiScore)
    {
        if (aiScore is null || humanScores.Count == 0)
            return false;

        decimal humanAverage = humanScores.Average(s => s.GetScorePercentage());
        decimal aiPercentage = aiScore.GetScorePercentage();

        return Math.Abs(humanAverage - aiPercentage) > VarianceThreshold;
    }

    /// <summary>
    /// Generates a heatmap data structure for all offers and criteria.
    /// Per PRD Section 9.3.
    /// </summary>
    /// <returns>
    /// A list of heatmap cells with offer blind code, criterion ID,
    /// average score percentage, and color classification.
    /// </returns>
    public static IReadOnlyList<HeatmapCell> GenerateHeatmap(
        IReadOnlyList<TechnicalScore> allScores,
        IReadOnlyList<SupplierOffer> offers,
        IReadOnlyList<EvaluationCriterion> criteria)
    {
        var cells = new List<HeatmapCell>();

        foreach (var offer in offers)
        {
            foreach (var criterion in criteria.Where(c => c.IsActive && c.ParentCriterionId == null))
            {
                var criterionScores = allScores
                    .Where(s => s.SupplierOfferId == offer.Id &&
                                s.EvaluationCriterionId == criterion.Id)
                    .ToList();

                decimal averagePercentage = criterionScores.Count > 0
                    ? criterionScores.Average(s => s.GetScorePercentage())
                    : 0m;

                var color = ClassifyScore(averagePercentage);

                cells.Add(new HeatmapCell(
                    offer.BlindCode,
                    offer.Id,
                    criterion.Id,
                    criterion.NameAr,
                    criterion.NameEn,
                    Math.Round(averagePercentage, 2),
                    color));
            }
        }

        return cells.AsReadOnly();
    }

    /// <summary>
    /// Classifies a score percentage into a color category for the heatmap.
    /// Per PRD Section 9.3: Green >= 80%, Yellow 60-79%, Red < 60%.
    /// </summary>
    public static HeatmapColor ClassifyScore(decimal percentage)
    {
        return percentage switch
        {
            >= 80m => HeatmapColor.Green,
            >= 60m => HeatmapColor.Yellow,
            _ => HeatmapColor.Red
        };
    }
}

/// <summary>
/// Represents a single cell in the technical evaluation heatmap.
/// </summary>
public sealed record HeatmapCell(
    string OfferBlindCode,
    Guid OfferId,
    Guid CriterionId,
    string CriterionNameAr,
    string CriterionNameEn,
    decimal AverageScorePercentage,
    HeatmapColor Color);

/// <summary>
/// Color classification for heatmap cells per PRD Section 9.3.
/// </summary>
public enum HeatmapColor
{
    /// <summary>Excellent: >= 80%</summary>
    Green = 1,

    /// <summary>Average: 60-79%</summary>
    Yellow = 2,

    /// <summary>Weak: < 60%</summary>
    Red = 3
}
