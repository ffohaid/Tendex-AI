using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;
using TendexAI.Domain.Services;

namespace TendexAI.Infrastructure.Tests.FinancialEvaluation;

/// <summary>
/// Unit tests for FinancialScoringService domain service.
/// Tests price comparison, deviation calculation, and ranking logic.
/// </summary>
public class FinancialScoringServiceTests
{
    [Theory]
    [InlineData(100, 120, 20.0)]
    [InlineData(100, 80, -20.0)]
    [InlineData(100, 100, 0.0)]
    [InlineData(200, 250, 25.0)]
    [InlineData(1000, 1150, 15.0)]
    public void CalculateDeviation_ShouldReturnCorrectPercentage(
        decimal estimated, decimal actual, decimal expectedDeviation)
    {
        // Act
        var result = FinancialScoringService.CalculateDeviation(estimated, actual);

        // Assert
        result.Should().Be(expectedDeviation);
    }

    [Fact]
    public void CalculateDeviation_WhenEstimatedIsZero_ShouldReturnZero()
    {
        // Act
        var result = FinancialScoringService.CalculateDeviation(0, 100);

        // Assert
        result.Should().Be(0m);
    }

    [Theory]
    [InlineData(0, PriceDeviationLevel.None)]
    [InlineData(3, PriceDeviationLevel.Low)]
    [InlineData(-3, PriceDeviationLevel.Low)]
    [InlineData(8, PriceDeviationLevel.Medium)]
    [InlineData(-8, PriceDeviationLevel.Medium)]
    [InlineData(18, PriceDeviationLevel.High)]
    [InlineData(-18, PriceDeviationLevel.High)]
    [InlineData(30, PriceDeviationLevel.Critical)]
    [InlineData(-30, PriceDeviationLevel.Critical)]
    public void ClassifyDeviation_ShouldReturnCorrectLevel(
        decimal deviation, PriceDeviationLevel expectedLevel)
    {
        // Act
        var result = FinancialScoringService.ClassifyDeviation(deviation);

        // Assert
        result.Should().Be(expectedLevel);
    }

    [Fact]
    public void VerifyArithmetic_WhenCorrect_ShouldReturnNoError()
    {
        // Arrange
        decimal unitPrice = 50m;
        decimal quantity = 10m;
        decimal submittedTotal = 500m;

        // Act
        var (hasError, calculatedTotal) =
            FinancialScoringService.VerifyArithmetic(unitPrice, quantity, submittedTotal);

        // Assert
        hasError.Should().BeFalse();
        calculatedTotal.Should().Be(500m);
    }

    [Fact]
    public void VerifyArithmetic_WhenIncorrect_ShouldReturnError()
    {
        // Arrange
        decimal unitPrice = 50m;
        decimal quantity = 10m;
        decimal submittedTotal = 600m; // Wrong

        // Act
        var (hasError, calculatedTotal) =
            FinancialScoringService.VerifyArithmetic(unitPrice, quantity, submittedTotal);

        // Assert
        hasError.Should().BeTrue();
        calculatedTotal.Should().Be(500m);
    }

    [Fact]
    public void VerifyArithmetic_WhenNoSubmittedTotal_ShouldReturnNoError()
    {
        // Arrange
        decimal unitPrice = 50m;
        decimal quantity = 10m;

        // Act
        var (hasError, calculatedTotal) =
            FinancialScoringService.VerifyArithmetic(unitPrice, quantity, null);

        // Assert
        hasError.Should().BeFalse();
        calculatedTotal.Should().Be(500m);
    }

    [Fact]
    public void CalculateFinancialScore_LowestPrice_ShouldGetHighestScore()
    {
        // Arrange
        decimal lowestAmount = 100_000m;
        decimal offerAmount = 100_000m;

        // Act
        var score = FinancialScoringService.CalculateFinancialScore(lowestAmount, offerAmount);

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
        var score = FinancialScoringService.CalculateFinancialScore(lowestAmount, offerAmount);

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
}
