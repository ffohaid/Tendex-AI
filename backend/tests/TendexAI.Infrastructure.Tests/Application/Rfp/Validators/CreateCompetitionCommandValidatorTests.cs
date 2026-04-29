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
        var command = CreateValidCommand();

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
    public void Validate_PastBookletIssueDate_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { BookletIssueDate = DateTime.UtcNow.AddDays(-1) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("bookletIssueDate");
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

    [Fact]
    public void Validate_ExcessiveInquiryPeriod_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { InquiryPeriodDays = 366 };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InquiryPeriodDays);
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
            BookletNumber = null,
            EstimatedBudget = null,
            BookletIssueDate = null,
            InquiriesStartDate = null,
            InquiryPeriodDays = null,
            OffersStartDate = null,
            SubmissionDeadline = null,
            ExpectedAwardDate = null,
            WorkStartDate = null,
            Department = null,
            FiscalYear = null,
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
        var bookletIssueDate = DateTime.UtcNow.Date.AddDays(10);

        return new CreateCompetitionCommand(
            TenantId: Guid.NewGuid(),
            ProjectNameAr: "مشروع اختبار",
            ProjectNameEn: "Test Project",
            Description: "وصف المشروع",
            CompetitionType: CompetitionType.PublicTender,
            CreationMethod: RfpCreationMethod.ManualWizard,
            BookletNumber: "BK-2026-001",
            EstimatedBudget: 1000000m,
            BookletIssueDate: bookletIssueDate,
            InquiriesStartDate: bookletIssueDate.AddDays(1),
            InquiryPeriodDays: 10,
            OffersStartDate: bookletIssueDate.AddDays(5),
            SubmissionDeadline: bookletIssueDate.AddDays(10),
            ExpectedAwardDate: bookletIssueDate.AddDays(20),
            WorkStartDate: bookletIssueDate.AddDays(25),
            Department: "Digital Transformation",
            FiscalYear: bookletIssueDate.Year.ToString(),
            SourceTemplateId: null,
            SourceCompetitionId: null,
            CreatedByUserId: Guid.NewGuid().ToString());
    }
}
