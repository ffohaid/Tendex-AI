using FluentAssertions;
using FluentValidation.TestHelper;
using TendexAI.Application.Features.Rfp.Commands.UpdateEvaluationSettings;

namespace TendexAI.Infrastructure.Tests.Application.Rfp.Validators;

/// <summary>
/// Unit tests for UpdateEvaluationSettingsCommandValidator.
/// </summary>
public class UpdateEvaluationSettingsCommandValidatorTests
{
    private readonly UpdateEvaluationSettingsCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidWeights_ShouldHaveNoErrors()
    {
        // Arrange
        var command = new UpdateEvaluationSettingsCommand(
            CompetitionId: Guid.NewGuid(),
            TechnicalPassingScore: 60m,
            TechnicalWeight: 70m,
            FinancialWeight: 30m,
            ModifiedByUserId: Guid.NewGuid().ToString());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WeightsNotSumTo100_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateEvaluationSettingsCommand(
            CompetitionId: Guid.NewGuid(),
            TechnicalPassingScore: 60m,
            TechnicalWeight: 70m,
            FinancialWeight: 40m,
            ModifiedByUserId: Guid.NewGuid().ToString());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("100"));
    }

    [Fact]
    public void Validate_TechnicalPassingScoreOutOfRange_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateEvaluationSettingsCommand(
            CompetitionId: Guid.NewGuid(),
            TechnicalPassingScore: 150m,
            TechnicalWeight: null,
            FinancialWeight: null,
            ModifiedByUserId: Guid.NewGuid().ToString());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TechnicalPassingScore);
    }

    [Fact]
    public void Validate_EmptyCompetitionId_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateEvaluationSettingsCommand(
            CompetitionId: Guid.Empty,
            TechnicalPassingScore: null,
            TechnicalWeight: null,
            FinancialWeight: null,
            ModifiedByUserId: Guid.NewGuid().ToString());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CompetitionId);
    }

    [Fact]
    public void Validate_NullWeights_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new UpdateEvaluationSettingsCommand(
            CompetitionId: Guid.NewGuid(),
            TechnicalPassingScore: null,
            TechnicalWeight: null,
            FinancialWeight: null,
            ModifiedByUserId: Guid.NewGuid().ToString());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
