using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;
using TendexAI.Domain.Services;

namespace TendexAI.Infrastructure.Tests.FinancialEvaluation;

/// <summary>
/// Unit tests for FinancialScoringService domain service.
/// Tests price comparison, deviation classification, and ranking logic.
/// </summary>
public class FinancialScoringServiceTests
{
    [Theory]
    [InlineData(0, PriceDeviationLevel.WithinRange)]
    [InlineData(3, PriceDeviationLevel.WithinRange)]
    [InlineData(-3, PriceDeviationLevel.WithinRange)]
    [InlineData(10, PriceDeviationLevel.WithinRange)]
    [InlineData(-10, PriceDeviationLevel.WithinRange)]
    [InlineData(18, PriceDeviationLevel.ModerateDeviation)]
    [InlineData(-18, PriceDeviationLevel.ModerateDeviation)]
    [InlineData(25, PriceDeviationLevel.ModerateDeviation)]
    [InlineData(30, PriceDeviationLevel.SignificantDeviation)]
    [InlineData(-30, PriceDeviationLevel.SignificantDeviation)]
    public void ClassifyDeviation_ShouldReturnCorrectLevel(
        decimal deviation, PriceDeviationLevel expectedLevel)
    {
        // Act
        var result = FinancialScoringService.ClassifyDeviation(deviation);

        // Assert
        result.Should().Be(expectedLevel);
    }

    [Fact]
    public void CalculateFinancialScore_LowestPrice_ShouldGetHighestScore()
    {
        // Arrange
        decimal lowestAmount = 100_000m;
        decimal offerAmount = 100_000m;

        // Act
        var score = FinancialScoringService.CalculateFinancialScore(offerAmount, lowestAmount);

        // Assert
        score.Should().Be(100m);
    }

    [Fact]
    public void CalculateFinancialScore_HigherPrice_ShouldGetLowerScore()
    {
        // Arrange
        decimal lowestAmount = 100_000m;
        decimal offerAmount = 200_000m;

        // Act
        var score = FinancialScoringService.CalculateFinancialScore(offerAmount, lowestAmount);

        // Assert
        score.Should().Be(50m);
    }

    [Fact]
    public void CalculateCombinedScore_ShouldApplyWeightsCorrectly()
    {
        // Arrange
        decimal technicalScore = 85m;
        decimal financialScore = 90m;
        decimal technicalWeight = 70m;
        decimal financialWeight = 30m;

        // Act
        var combined = FinancialScoringService.CalculateCombinedScore(
            technicalScore, financialScore, technicalWeight, financialWeight);

        // Assert
        // (85 * 70 / 100) + (90 * 30 / 100) = 59.5 + 27 = 86.5
        combined.Should().Be(86.50m);
    }

    [Fact]
    public void CalculateCombinedScore_WithEqualWeights_ShouldBeAverage()
    {
        // Arrange
        decimal technicalScore = 80m;
        decimal financialScore = 60m;

        // Act
        var combined = FinancialScoringService.CalculateCombinedScore(
            technicalScore, financialScore, 50m, 50m);

        // Assert
        combined.Should().Be(70m);
    }

    [Fact]
    public void CalculateFinancialScore_WhenZeroAmount_ShouldReturnZero()
    {
        // Act
        var score = FinancialScoringService.CalculateFinancialScore(0m, 100_000m);

        // Assert
        score.Should().Be(0m);
    }
}
