using FluentAssertions;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.EvaluationMinutes;

/// <summary>
/// Unit tests for EvaluationMinutes entity state transitions and business rules.
/// </summary>
public class EvaluationMinutesEntityTests
{
    private static Domain.Entities.Evaluation.EvaluationMinutes CreateTestMinutes(
        MinutesType type = MinutesType.FinalComprehensive)
    {
        return Domain.Entities.Evaluation.EvaluationMinutes.Create(
            competitionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            minutesType: type,
            titleAr: "محضر اختبار",
            contentJson: "{}",
            createdBy: "test-user");
    }

    [Fact]
    public void Create_ShouldInitializeWithDraftStatus()
    {
        // Act
        var minutes = CreateTestMinutes();

        // Assert
        minutes.Status.Should().Be(MinutesStatus.Draft);
        minutes.TitleAr.Should().Be("محضر اختبار");
        minutes.MinutesType.Should().Be(MinutesType.FinalComprehensive);
    }

    [Fact]
    public void SubmitForApproval_ShouldTransitionToPendingApproval()
    {
        // Arrange
        var minutes = CreateTestMinutes();

        // Act
        var result = minutes.SubmitForApproval("test-user");

        // Assert
        result.IsSuccess.Should().BeTrue();
        minutes.Status.Should().Be(MinutesStatus.PendingApproval);
    }

    [Fact]
    public void Approve_ShouldTransitionToApproved()
    {
        // Arrange
        var minutes = CreateTestMinutes();
        minutes.SubmitForApproval("test-user");

        // Act
        var result = minutes.Approve("approver-user");

        // Assert
        result.IsSuccess.Should().BeTrue();
        minutes.Status.Should().Be(MinutesStatus.Approved);
        minutes.ApprovedAt.Should().NotBeNull();
        minutes.ApprovedBy.Should().Be("approver-user");
    }

    [Fact]
    public void Approve_WhenNotPendingApproval_ShouldFail()
    {
        // Arrange
        var minutes = CreateTestMinutes();

        // Act
        var result = minutes.Approve("approver-user");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddSignatory_ShouldSucceed()
    {
        // Arrange
        var minutes = CreateTestMinutes();
        var signatory = Domain.Entities.Evaluation.MinutesSignatory.Create(
            minutes.Id, "user-1", "Ahmed Ali", "Committee Chair", "test-user");

        // Act
        minutes.AddSignatory(signatory);

        // Assert
        minutes.Signatories.Should().HaveCount(1);
    }

    [Fact]
    public void Create_TechnicalMinutes_ShouldHaveCorrectType()
    {
        // Act
        var minutes = CreateTestMinutes(MinutesType.TechnicalEvaluation);

        // Assert
        minutes.MinutesType.Should().Be(MinutesType.TechnicalEvaluation);
    }

    [Fact]
    public void Create_FinancialMinutes_ShouldHaveCorrectType()
    {
        // Act
        var minutes = CreateTestMinutes(MinutesType.FinancialEvaluation);

        // Assert
        minutes.MinutesType.Should().Be(MinutesType.FinancialEvaluation);
    }
}
