using FluentAssertions;
using FluentValidation.TestHelper;
using TendexAI.Application.Features.AI.Commands.GenerateBookletStructure;
using TendexAI.Application.Features.AI.Commands.GenerateBoq;
using TendexAI.Application.Features.AI.Commands.GenerateSectionDraft;
using TendexAI.Application.Features.AI.Commands.RefineBoq;
using TendexAI.Application.Features.AI.Commands.RefineSectionDraft;

namespace TendexAI.Infrastructure.Tests.AI;

/// <summary>
/// Unit tests for FluentValidation validators of specification drafting and BOQ commands.
/// </summary>
public sealed class SpecificationDraftingValidatorTests
{
    // ═══════════════════════════════════════════════════════════════
    //  GenerateSectionDraftCommandValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateSectionDraft_Should_Pass_With_Valid_Command()
    {
        // Arrange
        var validator = new GenerateSectionDraftCommandValidator();
        var command = new GenerateSectionDraftCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع تطوير نظام إلكتروني",
            ProjectDescriptionAr = "تطوير نظام إلكتروني متكامل",
            ProjectType = "تقنية معلومات",
            SectionType = "TechnicalSpecifications",
            SectionTitleAr = "المواصفات الفنية"
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GenerateSectionDraft_Should_Fail_With_Empty_TenantId()
    {
        var validator = new GenerateSectionDraftCommandValidator();
        var command = new GenerateSectionDraftCommand
        {
            TenantId = Guid.Empty,
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع",
            ProjectDescriptionAr = "وصف",
            ProjectType = "تقنية",
            SectionType = "TechnicalSpecifications",
            SectionTitleAr = "المواصفات"
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task GenerateSectionDraft_Should_Fail_With_Empty_ProjectName()
    {
        var validator = new GenerateSectionDraftCommandValidator();
        var command = new GenerateSectionDraftCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "",
            ProjectDescriptionAr = "وصف",
            ProjectType = "تقنية",
            SectionType = "TechnicalSpecifications",
            SectionTitleAr = "المواصفات"
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ProjectNameAr);
    }

    [Fact]
    public async Task GenerateSectionDraft_Should_Fail_With_Negative_Budget()
    {
        var validator = new GenerateSectionDraftCommandValidator();
        var command = new GenerateSectionDraftCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع",
            ProjectDescriptionAr = "وصف",
            ProjectType = "تقنية",
            EstimatedBudget = -1000,
            SectionType = "TechnicalSpecifications",
            SectionTitleAr = "المواصفات"
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.EstimatedBudget);
    }

    // ═══════════════════════════════════════════════════════════════
    //  RefineSectionDraftCommandValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task RefineSectionDraft_Should_Pass_With_Valid_Command()
    {
        var validator = new RefineSectionDraftCommandValidator();
        var command = new RefineSectionDraftCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع",
            SectionTitleAr = "المواصفات الفنية",
            CurrentContentHtml = "<p>محتوى</p>",
            UserFeedbackAr = "يرجى إضافة المزيد"
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task RefineSectionDraft_Should_Fail_With_Empty_Feedback()
    {
        var validator = new RefineSectionDraftCommandValidator();
        var command = new RefineSectionDraftCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع",
            SectionTitleAr = "المواصفات",
            CurrentContentHtml = "<p>محتوى</p>",
            UserFeedbackAr = ""
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.UserFeedbackAr);
    }

    [Fact]
    public async Task RefineSectionDraft_Should_Fail_With_Empty_CurrentContent()
    {
        var validator = new RefineSectionDraftCommandValidator();
        var command = new RefineSectionDraftCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع",
            SectionTitleAr = "المواصفات",
            CurrentContentHtml = "",
            UserFeedbackAr = "تعديل"
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.CurrentContentHtml);
    }

    // ═══════════════════════════════════════════════════════════════
    //  GenerateBookletStructureCommandValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateBookletStructure_Should_Pass_With_Valid_Command()
    {
        var validator = new GenerateBookletStructureCommandValidator();
        var command = new GenerateBookletStructureCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع",
            ProjectDescriptionAr = "وصف المشروع",
            ProjectType = "تقنية معلومات"
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GenerateBookletStructure_Should_Fail_With_Empty_ProjectType()
    {
        var validator = new GenerateBookletStructureCommandValidator();
        var command = new GenerateBookletStructureCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع",
            ProjectDescriptionAr = "وصف",
            ProjectType = ""
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ProjectType);
    }

    // ═══════════════════════════════════════════════════════════════
    //  GenerateBoqCommandValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GenerateBoq_Should_Pass_With_Valid_Command()
    {
        var validator = new GenerateBoqCommandValidator();
        var command = new GenerateBoqCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع",
            ProjectDescriptionAr = "وصف المشروع",
            ProjectType = "تقنية معلومات"
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GenerateBoq_Should_Fail_With_Empty_CompetitionId()
    {
        var validator = new GenerateBoqCommandValidator();
        var command = new GenerateBoqCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.Empty,
            ProjectNameAr = "مشروع",
            ProjectDescriptionAr = "وصف",
            ProjectType = "تقنية"
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.CompetitionId);
    }

    // ═══════════════════════════════════════════════════════════════
    //  RefineBoqCommandValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task RefineBoq_Should_Pass_With_Valid_Command()
    {
        var validator = new RefineBoqCommandValidator();
        var command = new RefineBoqCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع",
            ExistingBoqJson = "{}",
            UserFeedbackAr = "إضافة بنود"
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task RefineBoq_Should_Fail_With_Empty_ExistingBoq()
    {
        var validator = new RefineBoqCommandValidator();
        var command = new RefineBoqCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع",
            ExistingBoqJson = "",
            UserFeedbackAr = "تعديل"
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ExistingBoqJson);
    }

    [Fact]
    public async Task RefineBoq_Should_Fail_With_Empty_Feedback()
    {
        var validator = new RefineBoqCommandValidator();
        var command = new RefineBoqCommand
        {
            TenantId = Guid.NewGuid(),
            CompetitionId = Guid.NewGuid(),
            ProjectNameAr = "مشروع",
            ExistingBoqJson = "{}",
            UserFeedbackAr = ""
        };

        var result = await validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.UserFeedbackAr);
    }
}
