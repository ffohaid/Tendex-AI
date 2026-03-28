using FluentAssertions;
using FluentValidation.TestHelper;
using TendexAI.Application.Features.Rfp.Commands.CreateCompetition;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Application.Rfp.Validators;

/// <summary>
/// Unit tests for CreateCompetitionCommandValidator.
/// </summary>
public class CreateCompetitionCommandValidatorTests
{
    private readonly CreateCompetitionCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldHaveNoErrors()
    {
        // Arrange
        var command = new CreateCompetitionCommand(
            TenantId: Guid.NewGuid(),
            ProjectNameAr: "مشروع تطوير النظام",
            ProjectNameEn: "System Development Project",
            Description: "وصف المشروع",
            CompetitionType: CompetitionType.PublicTender,
            CreationMethod: RfpCreationMethod.ManualWizard,
            EstimatedBudget: 1000000m,
            SubmissionDeadline: DateTime.UtcNow.AddDays(30),
            ProjectDurationDays: 365,
            SourceTemplateId: null,
            SourceCompetitionId: null,
            CreatedByUserId: Guid.NewGuid().ToString());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyTenantId_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { TenantId = Guid.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void Validate_EmptyProjectNameAr_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ProjectNameAr = "" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectNameAr);
    }

    [Fact]
    public void Validate_EmptyProjectNameEn_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ProjectNameEn = "" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectNameEn);
    }

    [Fact]
    public void Validate_ProjectNameArTooLong_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ProjectNameAr = new string('أ', 501) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectNameAr);
    }

    [Fact]
    public void Validate_NegativeEstimatedBudget_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { EstimatedBudget = -100m };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EstimatedBudget);
    }

    [Fact]
    public void Validate_PastSubmissionDeadline_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SubmissionDeadline = DateTime.UtcNow.AddDays(-1) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubmissionDeadline);
    }

    [Fact]
    public void Validate_ZeroProjectDuration_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ProjectDurationDays = 0 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectDurationDays);
    }

    [Fact]
    public void Validate_ExcessiveProjectDuration_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ProjectDurationDays = 5000 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectDurationDays);
    }

    [Fact]
    public void Validate_EmptyCreatedByUserId_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { CreatedByUserId = "" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatedByUserId);
    }

    [Fact]
    public void Validate_NullOptionalFields_ShouldNotHaveErrors()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Description = null,
            EstimatedBudget = null,
            SubmissionDeadline = null,
            ProjectDurationDays = null,
            SourceTemplateId = null,
            SourceCompetitionId = null
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CreateCompetitionCommand CreateValidCommand()
    {
        return new CreateCompetitionCommand(
            TenantId: Guid.NewGuid(),
            ProjectNameAr: "مشروع اختبار",
            ProjectNameEn: "Test Project",
            Description: null,
            CompetitionType: CompetitionType.PublicTender,
            CreationMethod: RfpCreationMethod.ManualWizard,
            EstimatedBudget: null,
            SubmissionDeadline: null,
            ProjectDurationDays: null,
            SourceTemplateId: null,
            SourceCompetitionId: null,
            CreatedByUserId: Guid.NewGuid().ToString());
    }
}
