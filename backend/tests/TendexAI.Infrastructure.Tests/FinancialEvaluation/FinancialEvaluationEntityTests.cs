using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.FinancialEvaluation;

/// <summary>
/// Unit tests for FinancialEvaluation entity state transitions and business rules.
/// </summary>
public class FinancialEvaluationEntityTests
{
    private static TendexAI.Domain.Entities.Evaluation.FinancialEvaluation CreateTestEvaluation()
    {
        return TendexAI.Domain.Entities.Evaluation.FinancialEvaluation.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "test-user");
    }

    [Fact]
    public void Create_ShouldInitializeWithPendingStatus()
    {
        // Act
        var evaluation = CreateTestEvaluation();

        // Assert
        evaluation.Status.Should().Be(FinancialEvaluationStatus.Pending);
        evaluation.StartedAt.Should().BeNull();
        evaluation.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void StartEvaluation_ShouldTransitionToInProgress()
    {
        // Arrange
        var evaluation = CreateTestEvaluation();

        // Act
        var result = evaluation.StartEvaluation("test-user");

        // Assert
        result.IsSuccess.Should().BeTrue();
        evaluation.Status.Should().Be(FinancialEvaluationStatus.InProgress);
        evaluation.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public void StartEvaluation_WhenAlreadyStarted_ShouldFail()
    {
        // Arrange
        var evaluation = CreateTestEvaluation();
        evaluation.StartEvaluation("test-user");

        // Act
        var result = evaluation.StartEvaluation("test-user");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddOfferItem_ShouldSucceed_WhenInProgress()
    {
        // Arrange
        var evaluation = CreateTestEvaluation();
        evaluation.StartEvaluation("test-user");

        var item = FinancialOfferItem.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            100m, 5m, null, "test-user");

        // Act
        var result = evaluation.AddOfferItem(item);

        // Assert
        result.IsSuccess.Should().BeTrue();
        evaluation.OfferItems.Should().HaveCount(1);
    }

    [Fact]
    public void AddOfferItem_ShouldFail_WhenNotInProgress()
    {
        // Arrange
        var evaluation = CreateTestEvaluation();

        var item = FinancialOfferItem.Create(
            evaluation.Id, Guid.NewGuid(), Guid.NewGuid(),
            100m, 5m, null, "test-user");

        // Act
        var result = evaluation.AddOfferItem(item);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SubmitForApproval_ShouldTransitionToPendingApproval()
    {
        // Arrange
        var evaluation = CreateTestEvaluation();
        evaluation.StartEvaluation("test-user");
        evaluation.MarkAllScoresSubmitted("test-user");

        // Act
        var result = evaluation.SubmitForApproval("test-user");

        // Assert
        result.IsSuccess.Should().BeTrue();
        evaluation.Status.Should().Be(FinancialEvaluationStatus.PendingApproval);
    }

    [Fact]
    public void ApproveReport_ShouldTransitionToApproved()
    {
        // Arrange
        var evaluation = CreateTestEvaluation();
        evaluation.StartEvaluation("test-user");
        evaluation.MarkAllScoresSubmitted("test-user");
        evaluation.SubmitForApproval("test-user");

        // Act
        var result = evaluation.ApproveReport("approver-user");

        // Assert
        result.IsSuccess.Should().BeTrue();
        evaluation.Status.Should().Be(FinancialEvaluationStatus.Approved);
        evaluation.ApprovedAt.Should().NotBeNull();
        evaluation.ApprovedBy.Should().Be("approver-user");
        evaluation.IsReportApproved.Should().BeTrue();
    }

    [Fact]
    public void RejectReport_ShouldTransitionToRejected()
    {
        // Arrange
        var evaluation = CreateTestEvaluation();
        evaluation.StartEvaluation("test-user");
        evaluation.MarkAllScoresSubmitted("test-user");
        evaluation.SubmitForApproval("test-user");

        // Act
        var result = evaluation.RejectReport("approver-user", "Needs revision");

        // Assert
        result.IsSuccess.Should().BeTrue();
        evaluation.Status.Should().Be(FinancialEvaluationStatus.Rejected);
        evaluation.RejectionReason.Should().Be("Needs revision");
    }

    [Fact]
    public void RejectReport_WithoutReason_ShouldFail()
    {
        // Arrange
        var evaluation = CreateTestEvaluation();
        evaluation.StartEvaluation("test-user");
        evaluation.MarkAllScoresSubmitted("test-user");
        evaluation.SubmitForApproval("test-user");

        // Act
        var result = evaluation.RejectReport("approver-user", "");

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
