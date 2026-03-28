using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Domain.Services;

// HeatmapColor is defined in TendexAI.Domain.Services namespace

namespace TendexAI.Infrastructure.Tests.Domain.Evaluation;

/// <summary>
/// Unit tests for the TechnicalScoringService domain service.
/// Validates weighted score calculation, variance detection, and heatmap generation.
/// </summary>
public sealed class TechnicalScoringServiceTests
{
    private readonly Guid _evaluationId = Guid.NewGuid();
    private readonly Guid _competitionId = Guid.NewGuid();

    // ═══════════════════════════════════════════════════════════
    //  Weighted Score Calculation Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void CalculateWeightedTotalScore_Should_Apply_Weights_Correctly()
    {
        // Arrange: 2 criteria, each 50% weight
        // EvaluationCriterion.Create generates its own Id, so we must
        // create criteria first, then use their actual Ids for scores.
        var offerId = Guid.NewGuid();

        var criterion1 = CreateCriterion(50m);
        var criterion2 = CreateCriterion(50m);

        var criteria = new List<EvaluationCriterion> { criterion1, criterion2 };

        var scores = new List<TechnicalScore>
        {
            CreateScore(offerId, criterion1.Id, "eval-1", 80m, 100m),
            CreateScore(offerId, criterion1.Id, "eval-2", 90m, 100m),
            CreateScore(offerId, criterion2.Id, "eval-1", 60m, 100m),
            CreateScore(offerId, criterion2.Id, "eval-2", 70m, 100m)
        };

        // Act
        var result = TechnicalScoringService.CalculateWeightedTotalScore(scores, criteria);

        // Assert
        // Criterion 1: avg percentage = (80/100*100 + 90/100*100)/2 = 85%
        //   weighted = 85 * (50/100) = 42.5
        // Criterion 2: avg percentage = (60/100*100 + 70/100*100)/2 = 65%
        //   weighted = 65 * (50/100) = 32.5
        // Total weighted = 42.5 + 32.5 = 75, totalWeight = 100
        // Normalized = (75 / 100) * 100 = 75
        result.Should().Be(75m);
    }

    [Fact]
    public void CalculateWeightedTotalScore_Should_Handle_Unequal_Weights()
    {
        var offerId = Guid.NewGuid();

        var criterion1 = CreateCriterion(70m);
        var criterion2 = CreateCriterion(30m);

        var criteria = new List<EvaluationCriterion> { criterion1, criterion2 };

        var scores = new List<TechnicalScore>
        {
            CreateScore(offerId, criterion1.Id, "eval-1", 100m, 100m), // 100%
            CreateScore(offerId, criterion2.Id, "eval-1", 50m, 100m)   // 50%
        };

        var result = TechnicalScoringService.CalculateWeightedTotalScore(scores, criteria);

        // Criterion 1: 100% * (70/100) = 70
        // Criterion 2: 50% * (30/100) = 15
        // Total = 85, totalWeight = 100
        // Normalized = (85 / 100) * 100 = 85
        result.Should().Be(85m);
    }

    [Fact]
    public void CalculateWeightedTotalScore_Should_Return_Zero_For_No_Scores()
    {
        var criteria = new List<EvaluationCriterion>
        {
            CreateCriterion(50m)
        };

        var result = TechnicalScoringService.CalculateWeightedTotalScore(
            new List<TechnicalScore>(), criteria);

        result.Should().Be(0m);
    }

    // ═══════════════════════════════════════════════════════════
    //  Variance Detection Tests (20% threshold per PRD 9.2)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void HasEvaluatorVariance_Should_Return_True_When_Spread_Exceeds_20_Percent()
    {
        var offerId = Guid.NewGuid();
        var criterionId = Guid.NewGuid();

        var scores = new List<TechnicalScore>
        {
            CreateScore(offerId, criterionId, "eval-1", 50m, 100m), // 50%
            CreateScore(offerId, criterionId, "eval-2", 80m, 100m)  // 80% — spread = 30%
        };

        var result = TechnicalScoringService.HasEvaluatorVariance(scores);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasEvaluatorVariance_Should_Return_False_When_Spread_Within_20_Percent()
    {
        var offerId = Guid.NewGuid();
        var criterionId = Guid.NewGuid();

        var scores = new List<TechnicalScore>
        {
            CreateScore(offerId, criterionId, "eval-1", 75m, 100m), // 75%
            CreateScore(offerId, criterionId, "eval-2", 85m, 100m)  // 85% — spread = 10%
        };

        var result = TechnicalScoringService.HasEvaluatorVariance(scores);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasEvaluatorVariance_Should_Return_False_For_Single_Evaluator()
    {
        var scores = new List<TechnicalScore>
        {
            CreateScore(Guid.NewGuid(), Guid.NewGuid(), "eval-1", 80m, 100m)
        };

        var result = TechnicalScoringService.HasEvaluatorVariance(scores);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasHumanAiVariance_Should_Return_True_When_Difference_Exceeds_20_Percent()
    {
        var offerId = Guid.NewGuid();
        var criterionId = Guid.NewGuid();

        var humanScores = new List<TechnicalScore>
        {
            CreateScore(offerId, criterionId, "eval-1", 50m, 100m), // 50%
            CreateScore(offerId, criterionId, "eval-2", 60m, 100m)  // 60% — avg = 55%
        };

        var aiScore = AiTechnicalScore.Create(
            _evaluationId, offerId, criterionId,
            80m, 100m, "AI justification", null, "system"); // 80% — diff = 25%

        var result = TechnicalScoringService.HasHumanAiVariance(humanScores, aiScore);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasHumanAiVariance_Should_Return_False_When_Within_Threshold()
    {
        var offerId = Guid.NewGuid();
        var criterionId = Guid.NewGuid();

        var humanScores = new List<TechnicalScore>
        {
            CreateScore(offerId, criterionId, "eval-1", 75m, 100m),
            CreateScore(offerId, criterionId, "eval-2", 85m, 100m) // avg = 80%
        };

        var aiScore = AiTechnicalScore.Create(
            _evaluationId, offerId, criterionId,
            82m, 100m, "AI justification", null, "system"); // 82% — diff = 2%

        var result = TechnicalScoringService.HasHumanAiVariance(humanScores, aiScore);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasHumanAiVariance_Should_Return_False_When_No_AiScore()
    {
        var scores = new List<TechnicalScore>
        {
            CreateScore(Guid.NewGuid(), Guid.NewGuid(), "eval-1", 80m, 100m)
        };

        var result = TechnicalScoringService.HasHumanAiVariance(scores, null);

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════
    //  Heatmap Color Tests (per PRD 9.3)
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData(90, HeatmapColor.Green)]
    [InlineData(80, HeatmapColor.Green)]
    [InlineData(79, HeatmapColor.Yellow)]
    [InlineData(60, HeatmapColor.Yellow)]
    [InlineData(59, HeatmapColor.Red)]
    [InlineData(30, HeatmapColor.Red)]
    [InlineData(0, HeatmapColor.Red)]
    public void ClassifyScore_Should_Return_Correct_Color(decimal percentage, HeatmapColor expectedColor)
    {
        var result = TechnicalScoringService.ClassifyScore(percentage);

        result.Should().Be(expectedColor);
    }

    // ═══════════════════════════════════════════════════════════
    //  Pass/Fail Determination Tests
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData(60, 60, true)]
    [InlineData(61, 60, true)]
    [InlineData(59.99, 60, false)]
    [InlineData(0, 60, false)]
    [InlineData(100, 60, true)]
    public void DetermineResult_Should_Compare_Against_Minimum(
        decimal score, decimal minimum, bool expectedPass)
    {
        var result = TechnicalScoringService.DetermineResult(score, minimum);

        if (expectedPass)
            result.Should().Be(OfferTechnicalResult.Passed);
        else
            result.Should().Be(OfferTechnicalResult.Failed);
    }

    // ═══════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════

    private TechnicalScore CreateScore(
        Guid offerId, Guid criterionId, string evaluatorId,
        decimal score, decimal maxScore)
    {
        return TechnicalScore.Create(
            _evaluationId, offerId, criterionId,
            evaluatorId, score, maxScore, null, evaluatorId);
    }

    /// <summary>
    /// Creates an EvaluationCriterion with the given weight.
    /// The Id is auto-generated by the factory method.
    /// </summary>
    private EvaluationCriterion CreateCriterion(decimal weight)
    {
        return EvaluationCriterion.Create(
            _competitionId, "معيار", "Criterion",
            null, null, weight, null, 1, "system", null);
    }
}
