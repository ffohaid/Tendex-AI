using FluentAssertions;
using TendexAI.Application.Features.TechnicalEvaluation.Commands.ReviewAiAnalysis;
using TendexAI.Application.Features.TechnicalEvaluation.Commands.TriggerAiAnalysis;

namespace TendexAI.Infrastructure.Tests.Application.Evaluation;

/// <summary>
/// Unit tests for AI analysis command validators.
/// </summary>
public sealed class AiAnalysisValidatorTests
{
    // ═══════════════════════════════════════════════════════════════
    //  TriggerAiOfferAnalysisCommandValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TriggerValidator_Should_Pass_For_Valid_Command()
    {
        // Arrange
        var validator = new TriggerAiOfferAnalysisCommandValidator();
        var command = new TriggerAiOfferAnalysisCommand(Guid.NewGuid(), "user-1");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TriggerValidator_Should_Fail_When_EvaluationId_Is_Empty()
    {
        // Arrange
        var validator = new TriggerAiOfferAnalysisCommandValidator();
        var command = new TriggerAiOfferAnalysisCommand(Guid.Empty, "user-1");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EvaluationId");
    }

    [Fact]
    public void TriggerValidator_Should_Fail_When_TriggeredByUserId_Is_Empty()
    {
        // Arrange
        var validator = new TriggerAiOfferAnalysisCommandValidator();
        var command = new TriggerAiOfferAnalysisCommand(Guid.NewGuid(), "");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TriggeredByUserId");
    }

    // ═══════════════════════════════════════════════════════════════
    //  ReviewAiAnalysisCommandValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ReviewValidator_Should_Pass_For_Valid_Command()
    {
        // Arrange
        var validator = new ReviewAiAnalysisCommandValidator();
        var command = new ReviewAiAnalysisCommand(Guid.NewGuid(), "user-1", "ملاحظات المراجعة");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ReviewValidator_Should_Pass_When_ReviewNotes_Is_Null()
    {
        // Arrange
        var validator = new ReviewAiAnalysisCommandValidator();
        var command = new ReviewAiAnalysisCommand(Guid.NewGuid(), "user-1", null);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ReviewValidator_Should_Fail_When_AnalysisId_Is_Empty()
    {
        // Arrange
        var validator = new ReviewAiAnalysisCommandValidator();
        var command = new ReviewAiAnalysisCommand(Guid.Empty, "user-1", null);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AnalysisId");
    }

    [Fact]
    public void ReviewValidator_Should_Fail_When_ReviewedByUserId_Is_Empty()
    {
        // Arrange
        var validator = new ReviewAiAnalysisCommandValidator();
        var command = new ReviewAiAnalysisCommand(Guid.NewGuid(), "", null);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReviewedByUserId");
    }

    [Fact]
    public void ReviewValidator_Should_Fail_When_ReviewNotes_Exceeds_MaxLength()
    {
        // Arrange
        var validator = new ReviewAiAnalysisCommandValidator();
        var longNotes = new string('أ', 4001);
        var command = new ReviewAiAnalysisCommand(Guid.NewGuid(), "user-1", longNotes);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReviewNotes");
    }
}
