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
            supplierSubmittedTotal: null,
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
            supplierSubmittedTotal: 500m,
            createdBy: "test-user");

        // Act
        item.VerifyArithmetic("verifier-user");

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
            supplierSubmittedTotal: 600m, // Wrong: should be 500
            createdBy: "test-user");

        // Act
        item.VerifyArithmetic("verifier-user");

        // Assert
        item.IsArithmeticallyVerified.Should().BeTrue();
        item.HasArithmeticError.Should().BeTrue();
    }

    [Fact]
    public void SetDeviation_ShouldStoreValues()
    {
        // Arrange
        var item = FinancialOfferItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            unitPrice: 120m, quantity: 1m,
            supplierSubmittedTotal: null,
            createdBy: "test-user");

        // Act
        item.SetDeviation(20m, PriceDeviationLevel.High);

        // Assert
        item.DeviationPercentage.Should().Be(20m);
        item.DeviationLevel.Should().Be(PriceDeviationLevel.High);
    }

    [Fact]
    public void Create_WithLargeQuantity_ShouldCalculateCorrectly()
    {
        // Act
        var item = FinancialOfferItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            unitPrice: 1_500.50m, quantity: 1_000m,
            supplierSubmittedTotal: null,
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
            supplierSubmittedTotal: null,
            createdBy: "test-user");

        // Assert
        item.TotalPrice.Should().Be(99.99m);
    }
}
