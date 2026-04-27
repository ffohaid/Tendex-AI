using FluentAssertions;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Domain.Rfp;

/// <summary>
/// Unit tests for the Competition aggregate root domain entity.
/// Tests creation, status transitions, validation rules, and business logic.
/// 
/// State Machine Flow:
///   Draft → UnderPreparation → PendingApproval → Approved → Published → ...
///   Draft can only go to UnderPreparation or Cancelled.
///   UnderPreparation can go to PendingApproval, Draft, or Cancelled.
///   PendingApproval can go to Approved, Rejected, or Cancelled.
/// </summary>
public class CompetitionTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly string _userId = Guid.NewGuid().ToString();

    // ===== Creation Tests =====

    [Fact]
    public void Create_WithValidParameters_ShouldCreateCompetition()
    {
        // Act
        var competition = Competition.Create(
            tenantId: _tenantId,
            projectNameAr: "مشروع تطوير النظام",
            projectNameEn: "System Development Project",
            competitionType: CompetitionType.PublicTender,
            creationMethod: RfpCreationMethod.ManualWizard,
            createdByUserId: _userId);

        // Assert
        competition.Should().NotBeNull();
        competition.Id.Should().NotBeEmpty();
        competition.TenantId.Should().Be(_tenantId);
        competition.ProjectNameAr.Should().Be("مشروع تطوير النظام");
        competition.ProjectNameEn.Should().Be("System Development Project");
        competition.CompetitionType.Should().Be(CompetitionType.PublicTender);
        competition.CreationMethod.Should().Be(RfpCreationMethod.ManualWizard);
        competition.Status.Should().Be(CompetitionStatus.Draft);
        competition.ReferenceNumber.Should().StartWith("RFP-");
        competition.Currency.Should().Be("SAR");
        competition.Version.Should().Be(1);
        competition.CurrentWizardStep.Should().Be(1);
        competition.IsDeleted.Should().BeFalse();
        competition.CreatedBy.Should().Be(_userId);
    }

    [Fact]
    public void Create_WithTemplateSource_ShouldSetSourceTemplateId()
    {
        // Arrange
        var templateId = Guid.NewGuid();

        // Act
        var competition = Competition.Create(
            tenantId: _tenantId,
            projectNameAr: "مشروع",
            projectNameEn: "Project",
            competitionType: CompetitionType.PublicTender,
            creationMethod: RfpCreationMethod.FromTemplate,
            createdByUserId: _userId,
            sourceTemplateId: templateId);

        // Assert
        competition.SourceTemplateId.Should().Be(templateId);
        competition.CreationMethod.Should().Be(RfpCreationMethod.FromTemplate);
    }

    // ===== UpdateBasicInfo Tests =====

    [Fact]
    public void UpdateBasicInfo_OnDraftCompetition_ShouldSucceed()
    {
        // Arrange
        var competition = CreateDraftCompetition();

        // Act
        var result = competition.UpdateBasicInfo(
            projectNameAr: "اسم محدث",
            projectNameEn: "Updated Name",
            description: "وصف جديد",
            competitionType: CompetitionType.LimitedTender,
            estimatedBudget: 500000m,
            submissionDeadline: DateTime.UtcNow.AddDays(30),
            projectDurationDays: 180,
            startDate: null,
            endDate: null,
            department: null,
            fiscalYear: null,
            modifiedBy: _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        competition.ProjectNameAr.Should().Be("اسم محدث");
        competition.ProjectNameEn.Should().Be("Updated Name");
        competition.EstimatedBudget.Should().Be(500000m);
        competition.ProjectDurationDays.Should().Be(180);
        competition.Version.Should().Be(2);
    }

    [Fact]
    public void UpdateBasicInfo_OnApprovedCompetition_ShouldFail()
    {
        // Arrange — follow the correct state machine path:
        // Draft → UnderPreparation → PendingApproval → Approved
        var competition = CreateApprovedCompetition();

        // Act
        var result = competition.UpdateBasicInfo(
            projectNameAr: "اسم محدث",
            projectNameEn: "Updated Name",
            description: null,
            competitionType: CompetitionType.PublicTender,
            estimatedBudget: null,
            submissionDeadline: null,
            projectDurationDays: null,
            startDate: null,
            endDate: null,
            department: null,
            fiscalYear: null,
            modifiedBy: _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("لا يمكن تحديث المعلومات الأساسية");
    }

    // ===== Status Transition Tests =====

    [Fact]
    public void TransitionTo_UnderPreparation_FromDraft_ShouldSucceed()
    {
        // Arrange
        var competition = CreateDraftCompetition();

        // Act — Draft → UnderPreparation is the first valid transition
        var result = competition.TransitionTo(CompetitionStatus.UnderPreparation, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.UnderPreparation);
    }

    [Fact]
    public void SubmitForApproval_FromUnderPreparation_ShouldSucceed()
    {
        // Arrange — must go Draft → UnderPreparation first, then submit
        var competition = CreateUnderPreparationCompetitionWithSection();

        // Act
        var result = competition.SubmitForApproval(_userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.PendingApproval);
    }

    [Fact]
    public void SubmitForApproval_FromDraft_ShouldFail()
    {
        // Arrange — Draft cannot go directly to PendingApproval
        var competition = CreateDraftCompetitionWithSection();

        // Act
        var result = competition.SubmitForApproval(_userId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SubmitForApproval_FromApproved_ShouldFail()
    {
        // Arrange
        var competition = CreateApprovedCompetition();

        // Act
        var result = competition.SubmitForApproval(_userId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Approve_FromPendingApproval_ShouldSucceed()
    {
        // Arrange — Draft → UnderPreparation → PendingApproval
        var competition = CreatePendingApprovalCompetition();

        // Act
        var result = competition.Approve(_userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.Approved);
        competition.ApprovedByUserId.Should().Be(_userId);
        competition.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_FromDraft_ShouldFail()
    {
        // Arrange
        var competition = CreateDraftCompetition();

        // Act
        var result = competition.Approve(_userId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reject_FromPendingApproval_ShouldSucceed()
    {
        // Arrange — Draft → UnderPreparation → PendingApproval
        var competition = CreatePendingApprovalCompetition();

        // Act
        var result = competition.Reject(_userId, "Does not meet requirements.");

        // Assert
        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.Rejected);
        competition.StatusChangeReason.Should().Be("Does not meet requirements.");
    }

    [Fact]
    public void Cancel_FromDraft_ShouldSucceed()
    {
        // Arrange
        var competition = CreateDraftCompetition();

        // Act
        var result = competition.Cancel(_userId, "No longer needed.");

        // Assert
        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.Cancelled);
        competition.StatusChangeReason.Should().Be("No longer needed.");
    }

    // ===== Soft Delete Tests =====

    [Fact]
    public void SoftDelete_DraftCompetition_ShouldSucceed()
    {
        // Arrange
        var competition = CreateDraftCompetition();

        // Act
        var result = competition.SoftDelete(_userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        competition.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_ApprovedCompetition_ShouldFail()
    {
        // Arrange — follow the correct state machine path
        var competition = CreateApprovedCompetition();

        // Act
        var result = competition.SoftDelete(_userId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    // ===== Auto-Save Tests =====

    [Fact]
    public void RecordAutoSave_ShouldUpdateTimestampAndVersion()
    {
        // Arrange
        var competition = CreateDraftCompetition();
        var initialVersion = competition.Version;

        // Act
        competition.RecordAutoSave(wizardStep: 3);

        // Assert
        competition.LastAutoSavedAt.Should().NotBeNull();
        competition.CurrentWizardStep.Should().Be(3);
        competition.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void RecordAutoSave_WithoutWizardStep_ShouldKeepCurrentStep()
    {
        // Arrange
        var competition = CreateDraftCompetition();

        // Act
        competition.RecordAutoSave(wizardStep: null);

        // Assert
        competition.LastAutoSavedAt.Should().NotBeNull();
        competition.CurrentWizardStep.Should().Be(1); // Default
    }

    // ===== Section Management Tests =====

    [Fact]
    public void AddSection_ToDraftCompetition_ShouldSucceed()
    {
        // Arrange
        var competition = CreateDraftCompetition();
        var section = CreateTestSection(competition.Id);

        // Act
        var result = competition.AddSection(section);

        // Assert
        result.IsSuccess.Should().BeTrue();
        competition.Sections.Should().HaveCount(1);
        competition.Sections.First().TitleAr.Should().Be("قسم اختبار");
    }

    [Fact]
    public void AddSection_ToApprovedCompetition_ShouldFail()
    {
        // Arrange — follow the correct state machine path
        var competition = CreateApprovedCompetition();
        var section = CreateTestSection(competition.Id);

        // Act
        var result = competition.AddSection(section);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    // ===== BOQ Item Tests =====

    [Fact]
    public void AddBoqItem_ToDraftCompetition_ShouldSucceed()
    {
        // Arrange
        var competition = CreateDraftCompetition();
        var item = BoqItem.Create(
            competitionId: competition.Id,
            itemNumber: "1.1",
            descriptionAr: "توريد أجهزة حاسب",
            descriptionEn: "Computer Supply",
            unit: BoqItemUnit.Each,
            quantity: 50,
            estimatedUnitPrice: 5000m,
            category: "IT Equipment",
            createdBy: _userId,
            sortOrder: 1);

        // Act
        var result = competition.AddBoqItem(item);

        // Assert
        result.IsSuccess.Should().BeTrue();
        competition.BoqItems.Should().HaveCount(1);
    }

    // ===== Evaluation Criterion Tests =====

    [Fact]
    public void AddEvaluationCriterion_ToDraftCompetition_ShouldSucceed()
    {
        // Arrange
        var competition = CreateDraftCompetition();
        var criterion = EvaluationCriterion.Create(
            competitionId: competition.Id,
            nameAr: "الخبرة السابقة",
            nameEn: "Previous Experience",
            descriptionAr: "تقييم الخبرة",
            descriptionEn: "Experience evaluation",
            weightPercentage: 30m,
            minimumPassingScore: 60m,
            sortOrder: 1,
            createdBy: _userId);

        // Act
        var result = competition.AddEvaluationCriterion(criterion);

        // Assert
        result.IsSuccess.Should().BeTrue();
        competition.EvaluationCriteria.Should().HaveCount(1);
    }

    // ===== Evaluation Settings Tests =====

    [Fact]
    public void UpdateEvaluationSettings_ValidWeights_ShouldSucceed()
    {
        // Arrange
        var competition = CreateDraftCompetition();

        // Act
        var result = competition.UpdateEvaluationSettings(
            technicalPassingScore: 60m,
            technicalWeight: 70m,
            financialWeight: 30m,
            modifiedBy: _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        competition.TechnicalPassingScore.Should().Be(60m);
        competition.TechnicalWeight.Should().Be(70m);
        competition.FinancialWeight.Should().Be(30m);
    }

    [Fact]
    public void UpdateEvaluationSettings_WeightsNotSumTo100_ShouldFail()
    {
        // Arrange
        var competition = CreateDraftCompetition();

        // Act
        var result = competition.UpdateEvaluationSettings(
            technicalPassingScore: 60m,
            technicalWeight: 70m,
            financialWeight: 40m,
            modifiedBy: _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("100");
    }

    // ===== Helper Methods =====

    private Competition CreateDraftCompetition()
    {
        return Competition.Create(
            tenantId: _tenantId,
            projectNameAr: "مشروع اختبار",
            projectNameEn: "Test Project",
            competitionType: CompetitionType.PublicTender,
            creationMethod: RfpCreationMethod.ManualWizard,
            createdByUserId: _userId);
    }

    private RfpSection CreateTestSection(Guid competitionId)
    {
        return RfpSection.Create(
            competitionId: competitionId,
            titleAr: "قسم اختبار",
            titleEn: "Test Section",
            sectionType: RfpSectionType.GeneralInformation,
            contentHtml: "<p>Content</p>",
            isMandatory: false,
            isFromTemplate: false,
            defaultTextColor: TextColorType.Mandatory,
            createdBy: _userId);
    }

    private Competition CreateDraftCompetitionWithSection()
    {
        var competition = CreateDraftCompetition();
        competition.AddSection(CreateTestSection(competition.Id));
        return competition;
    }

    /// <summary>
    /// Creates a competition in UnderPreparation status with a section.
    /// Path: Draft → UnderPreparation
    /// </summary>
    private Competition CreateUnderPreparationCompetitionWithSection()
    {
        var competition = CreateDraftCompetitionWithSection();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, _userId);
        return competition;
    }

    /// <summary>
    /// Creates a competition in PendingApproval status.
    /// Path: Draft → UnderPreparation → PendingApproval
    /// </summary>
    private Competition CreatePendingApprovalCompetition()
    {
        var competition = CreateUnderPreparationCompetitionWithSection();
        competition.SubmitForApproval(_userId);
        return competition;
    }

    /// <summary>
    /// Creates a competition in Approved status.
    /// Path: Draft → UnderPreparation → PendingApproval → Approved
    /// </summary>
    private Competition CreateApprovedCompetition()
    {
        var competition = CreatePendingApprovalCompetition();
        competition.Approve(_userId);
        return competition;
    }
}
