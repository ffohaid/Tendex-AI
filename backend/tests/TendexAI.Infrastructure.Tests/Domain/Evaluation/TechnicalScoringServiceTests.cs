using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Services;

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
        var offerId = Guid.NewGuid();
        var criterion1Id = Guid.NewGuid();
        var criterion2Id = Guid.NewGuid();

        var criteria = new List<EvaluationCriterion>
        {
            CreateCriterion(criterion1Id, 50m, 100m),
            CreateCriterion(criterion2Id, 50m, 100m)
        };

        var scores = new List<TechnicalScore>
        {
            CreateScore(offerId, criterion1Id, "eval-1", 80m, 100m),
            CreateScore(offerId, criterion1Id, "eval-2", 90m, 100m),
            CreateScore(offerId, criterion2Id, "eval-1", 60m, 100m),
            CreateScore(offerId, criterion2Id, "eval-2", 70m, 100m)
        };

        // Act
        var result = TechnicalScoringService.CalculateWeightedTotalScore(scores, criteria);

        // Assert
        // Criterion 1: avg = (80+90)/2 = 85, percentage = 85%, weighted = 85% * 50 = 42.5
        // Criterion 2: avg = (60+70)/2 = 65, percentage = 65%, weighted = 65% * 50 = 32.5
        // Total = 42.5 + 32.5 = 75
        result.Should().Be(75m);
    }

    [Fact]
    public void CalculateWeightedTotalScore_Should_Handle_Unequal_Weights()
    {
        var offerId = Guid.NewGuid();
        var criterion1Id = Guid.NewGuid();
        var criterion2Id = Guid.NewGuid();

        var criteria = new List<EvaluationCriterion>
        {
            CreateCriterion(criterion1Id, 70m, 100m),
            CreateCriterion(criterion2Id, 30m, 100m)
        };

        var scores = new List<TechnicalScore>
        {
            CreateScore(offerId, criterion1Id, "eval-1", 100m, 100m), // 100%
            CreateScore(offerId, criterion2Id, "eval-1", 50m, 100m)   // 50%
        };

        var result = TechnicalScoringService.CalculateWeightedTotalScore(scores, criteria);

        // 100% * 70 + 50% * 30 = 70 + 15 = 85
        result.Should().Be(85m);
    }

    [Fact]
    public void CalculateWeightedTotalScore_Should_Return_Zero_For_No_Scores()
    {
        var criteria = new List<EvaluationCriterion>
        {
            CreateCriterion(Guid.NewGuid(), 50m, 100m)
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
    [InlineData(90, "green")]
    [InlineData(80, "green")]
    [InlineData(79, "yellow")]
    [InlineData(60, "yellow")]
    [InlineData(59, "red")]
    [InlineData(30, "red")]
    [InlineData(0, "red")]
    public void GetHeatmapColor_Should_Return_Correct_Color(decimal percentage, string expectedColor)
    {
        var result = TechnicalScoringService.GetHeatmapColor(percentage);

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
    public void DeterminePassFail_Should_Compare_Against_Minimum(
        decimal score, decimal minimum, bool expectedPass)
    {
        var result = TechnicalScoringService.DeterminePassFail(score, minimum);

        result.Should().Be(expectedPass);
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

    private EvaluationCriterion CreateCriterion(
        Guid id, decimal weight, decimal maxScore)
    {
        return EvaluationCriterion.Create(
            _competitionId, "معيار", "Criterion",
            null, null, weight, 0m, maxScore, 1, null, "system", id);
    }
}
