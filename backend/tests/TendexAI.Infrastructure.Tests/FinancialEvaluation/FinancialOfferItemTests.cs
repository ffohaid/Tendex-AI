using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.FinancialEvaluation;

/// <summary>
/// Unit tests for FinancialOfferItem entity - arithmetic verification and deviation.
/// </summary>
public class FinancialOfferItemTests
{
    [Fact]
    public void Create_ShouldCalculateTotalPrice()
    {
        // Act
        var item = FinancialOfferItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            unitPrice: 100m, quantity: 5m,
            createdBy: "test-user");

        // Assert
        item.TotalPrice.Should().Be(500m);
        item.UnitPrice.Should().Be(100m);
        item.Quantity.Should().Be(5m);
    }

    [Fact]
    public void VerifyArithmetic_WhenCorrect_ShouldNotFlagError()
    {
        // Arrange
        var item = FinancialOfferItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            unitPrice: 50m, quantity: 10m,
            createdBy: "test-user");

        // Act
        item.VerifyArithmetic(500m, "verifier-user");

        // Assert
        item.IsArithmeticallyVerified.Should().BeTrue();
        item.HasArithmeticError.Should().BeFalse();
    }

    [Fact]
    public void VerifyArithmetic_WhenIncorrect_ShouldFlagError()
    {
        // Arrange
        var item = FinancialOfferItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            unitPrice: 50m, quantity: 10m,
            createdBy: "test-user");

        // Act
        item.VerifyArithmetic(600m, "verifier-user"); // Wrong: should be 500

        // Assert
        item.IsArithmeticallyVerified.Should().BeTrue();
        item.HasArithmeticError.Should().BeTrue();
    }

    [Fact]
    public void CalculateDeviation_ShouldStoreValues()
    {
        // Arrange
        var item = FinancialOfferItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            unitPrice: 120m, quantity: 1m,
            createdBy: "test-user");

        // Act
        item.CalculateDeviation(100m, "test-user");

        // Assert
        item.DeviationPercentage.Should().Be(20m);
        item.DeviationLevel.Should().Be(PriceDeviationLevel.ModerateDeviation);
    }

    [Fact]
    public void Create_WithLargeQuantity_ShouldCalculateCorrectly()
    {
        // Act
        var item = FinancialOfferItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            unitPrice: 1_500.50m, quantity: 1_000m,
            createdBy: "test-user");

        // Assert
        item.TotalPrice.Should().Be(1_500_500m);
    }

    [Fact]
    public void Create_WithDecimalPrecision_ShouldMaintainAccuracy()
    {
        // Act
        var item = FinancialOfferItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            unitPrice: 33.33m, quantity: 3m,
            createdBy: "test-user");

        // Assert
        item.TotalPrice.Should().Be(99.99m);
    }
}
