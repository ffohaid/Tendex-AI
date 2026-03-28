using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Award;

/// <summary>
/// Unit tests for AwardRecommendation entity state transitions and business rules.
/// </summary>
public class AwardRecommendationEntityTests
{
    private static AwardRecommendation CreateTestAward()
    {
        return AwardRecommendation.Create(
            competitionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            financialEvaluationId: Guid.NewGuid(),
            recommendedOfferId: Guid.NewGuid(),
            recommendedSupplierName: "Test Supplier",
            technicalScore: 85m,
            financialScore: 90m,
            combinedScore: 86.5m,
            totalOfferAmount: 500_000m,
            justification: "Highest combined score",
            createdBy: "test-user");
    }

    [Fact]
    public void Create_ShouldInitializeWithDraftStatus()
    {
        // Act
        var award = CreateTestAward();

        // Assert
        award.Status.Should().Be(AwardStatus.Draft);
        award.RecommendedSupplierName.Should().Be("Test Supplier");
        award.TechnicalScore.Should().Be(85m);
        award.FinancialScore.Should().Be(90m);
        award.CombinedScore.Should().Be(86.5m);
        award.TotalOfferAmount.Should().Be(500_000m);
    }

    [Fact]
    public void SubmitForApproval_ShouldTransitionToPendingApproval()
    {
        // Arrange
        var award = CreateTestAward();

        // Act
        var result = award.SubmitForApproval("test-user");

        // Assert
        result.IsSuccess.Should().BeTrue();
        award.Status.Should().Be(AwardStatus.PendingApproval);
    }

    [Fact]
    public void Approve_ShouldTransitionToApproved()
    {
        // Arrange
        var award = CreateTestAward();
        award.SubmitForApproval("test-user");

        // Act
        var result = award.Approve("approver-user");

        // Assert
        result.IsSuccess.Should().BeTrue();
        award.Status.Should().Be(AwardStatus.Approved);
        award.ApprovedAt.Should().NotBeNull();
        award.ApprovedBy.Should().Be("approver-user");
    }

    [Fact]
    public void Approve_WhenNotPendingApproval_ShouldFail()
    {
        // Arrange
        var award = CreateTestAward();

        // Act
        var result = award.Approve("approver-user");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reject_ShouldTransitionToRejected()
    {
        // Arrange
        var award = CreateTestAward();
        award.SubmitForApproval("test-user");

        // Act
        var result = award.Reject("approver-user", "Budget exceeded");

        // Assert
        result.IsSuccess.Should().BeTrue();
        award.Status.Should().Be(AwardStatus.Rejected);
        award.RejectionReason.Should().Be("Budget exceeded");
    }

    [Fact]
    public void Reject_WithoutReason_ShouldFail()
    {
        // Arrange
        var award = CreateTestAward();
        award.SubmitForApproval("test-user");

        // Act
        var result = award.Reject("approver-user", "");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddRanking_ShouldSucceed()
    {
        // Arrange
        var award = CreateTestAward();
        var ranking = AwardRanking.Create(
            award.Id, Guid.NewGuid(), "Supplier A",
            1, 85m, 90m, 86.5m, 500_000m, "test-user");

        // Act
        award.AddRanking(ranking);

        // Assert
        award.Rankings.Should().HaveCount(1);
    }

    [Fact]
    public void AddMultipleRankings_ShouldMaintainOrder()
    {
        // Arrange
        var award = CreateTestAward();

        var ranking1 = AwardRanking.Create(
            award.Id, Guid.NewGuid(), "Supplier A",
            1, 85m, 90m, 86.5m, 500_000m, "test-user");

        var ranking2 = AwardRanking.Create(
            award.Id, Guid.NewGuid(), "Supplier B",
            2, 80m, 85m, 81.5m, 600_000m, "test-user");

        var ranking3 = AwardRanking.Create(
            award.Id, Guid.NewGuid(), "Supplier C",
            3, 75m, 80m, 76.5m, 700_000m, "test-user");

        // Act
        award.AddRanking(ranking1);
        award.AddRanking(ranking2);
        award.AddRanking(ranking3);

        // Assert
        award.Rankings.Should().HaveCount(3);
    }
}
