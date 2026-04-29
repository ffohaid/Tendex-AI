using System.Globalization;
using FluentValidation.TestHelper;
using TendexAI.Application.Features.Rfp.Commands.AutoSaveCompetition;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Application.Rfp.Validators;

/// <summary>
/// Unit tests for AutoSaveCompetitionCommandValidator.
/// Ensures lightweight validation for auto-save operations.
/// </summary>
public class AutoSaveCompetitionCommandValidatorTests
{
    private readonly AutoSaveCompetitionCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidAutoSaveCommand_ShouldHaveNoErrors()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyCompetitionId_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { CompetitionId = Guid.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CompetitionId);
    }

    [Fact]
    public void Validate_EmptyModifiedByUserId_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ModifiedByUserId = "" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ModifiedByUserId);
    }

    [Fact]
    public void Validate_WizardStepOutOfRange_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { CurrentWizardStep = 10 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentWizardStep);
    }

    [Fact]
    public void Validate_WizardStepZero_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { CurrentWizardStep = 0 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentWizardStep);
    }

    [Fact]
    public void Validate_NullOptionalFields_ShouldNotHaveErrors()
    {
        // Arrange - All optional fields null
        var command = new AutoSaveCompetitionCommand(
            CompetitionId: Guid.NewGuid(),
            ProjectNameAr: null,
            ProjectNameEn: null,
            Description: null,
            CompetitionType: null,
            BookletNumber: null,
            EstimatedBudget: null,
            BookletIssueDate: null,
            InquiriesStartDate: null,
            InquiryPeriodDays: null,
            OffersStartDate: null,
            SubmissionDeadline: null,
            ExpectedAwardDate: null,
            WorkStartDate: null,
            Department: null,
            FiscalYear: null,
            RequiredAttachmentTypes: null,
            CurrentWizardStep: null,
            ModifiedByUserId: Guid.NewGuid().ToString());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
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
    public void Validate_InvalidBookletNumber_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { BookletNumber = "BK#001" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BookletNumber);
    }

    [Fact]
    public void Validate_ZeroInquiryPeriod_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { InquiryPeriodDays = 0 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InquiryPeriodDays);
    }

    private static AutoSaveCompetitionCommand CreateValidCommand()
    {
        var bookletIssueDate = DateTime.UtcNow.Date.AddDays(10);

        return new AutoSaveCompetitionCommand(
            CompetitionId: Guid.NewGuid(),
            ProjectNameAr: "مشروع",
            ProjectNameEn: "Project",
            Description: "وصف مختصر",
            CompetitionType: CompetitionType.PublicTender,
            BookletNumber: "BK-2026-002",
            EstimatedBudget: 500000m,
            BookletIssueDate: bookletIssueDate,
            InquiriesStartDate: bookletIssueDate.AddDays(1),
            InquiryPeriodDays: 7,
            OffersStartDate: bookletIssueDate.AddDays(4),
            SubmissionDeadline: bookletIssueDate.AddDays(8),
            ExpectedAwardDate: bookletIssueDate.AddDays(15),
            WorkStartDate: bookletIssueDate.AddDays(20),
            Department: "Procurement",
            FiscalYear: bookletIssueDate.Year.ToString(CultureInfo.InvariantCulture),
            RequiredAttachmentTypes: null,
            CurrentWizardStep: 2,
            ModifiedByUserId: Guid.NewGuid().ToString());
    }
}
