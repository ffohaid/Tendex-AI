using FluentAssertions;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Domain.Events;

namespace TendexAI.Infrastructure.Tests.Domain.Rfp;

/// <summary>
/// Unit tests for Competition aggregate state transitions.
/// Tests the TransitionTo method and legacy convenience methods.
/// </summary>
public sealed class CompetitionTransitionTests
{
    private static Competition CreateDraftCompetition()
    {
        return Competition.Create(
            tenantId: Guid.NewGuid(),
            projectNameAr: "مشروع اختبار",
            projectNameEn: "Test Project",
            competitionType: CompetitionType.PublicTender,
            creationMethod: RfpCreationMethod.ManualWizard,
            createdByUserId: "user-1");
    }

    private static Competition CreateCompetitionWithSections()
    {
        var competition = CreateDraftCompetition();
        var section = RfpSection.Create(
            competition.Id, "Section 1", "القسم 1",
            RfpSectionType.GeneralInformation, "<p>Content</p>",
            isMandatory: true, isFromTemplate: false,
            TextColorType.Editable, "user-1");
        competition.AddSection(section);
        return competition;
    }

    // ═══════════════════════════════════════════════════════════════
    //  TransitionTo — Valid Transitions
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionTo_DraftToUnderPreparation_Succeeds()
    {
        var competition = CreateDraftCompetition();

        var result = competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");

        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.UnderPreparation);
        competition.CurrentPhase.Should().Be(CompetitionPhase.BookletPreparation);
    }

    [Fact]
    public void TransitionTo_UnderPreparationToPendingApproval_Succeeds()
    {
        var competition = CreateDraftCompetition();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");

        var result = competition.TransitionTo(CompetitionStatus.PendingApproval, "user-1");

        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.PendingApproval);
        competition.CurrentPhase.Should().Be(CompetitionPhase.BookletApproval);
    }

    [Fact]
    public void TransitionTo_PendingApprovalToApproved_SetsApprovalFields()
    {
        var competition = CreateDraftCompetition();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");
        competition.TransitionTo(CompetitionStatus.PendingApproval, "user-1");

        var result = competition.TransitionTo(CompetitionStatus.Approved, "approver-1");

        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.Approved);
        competition.ApprovedByUserId.Should().Be("approver-1");
        competition.ApprovedAt.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════
    //  TransitionTo — Invalid Transitions (Stage Skipping)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionTo_DraftToPublished_Fails()
    {
        var competition = CreateDraftCompetition();

        var result = competition.TransitionTo(CompetitionStatus.Published, "user-1");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid status transition");
        competition.Status.Should().Be(CompetitionStatus.Draft);
    }

    [Fact]
    public void TransitionTo_DraftToTechnicalAnalysis_Fails()
    {
        var competition = CreateDraftCompetition();

        var result = competition.TransitionTo(CompetitionStatus.TechnicalAnalysis, "user-1");

        result.IsFailure.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.Draft);
    }

    [Fact]
    public void TransitionTo_DraftToContractSigned_Fails()
    {
        var competition = CreateDraftCompetition();

        var result = competition.TransitionTo(CompetitionStatus.ContractSigned, "user-1");

        result.IsFailure.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.Draft);
    }

    // ═══════════════════════════════════════════════════════════════
    //  TransitionTo — Exception States
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionTo_Cancelled_RequiresReason()
    {
        var competition = CreateDraftCompetition();

        var result = competition.TransitionTo(CompetitionStatus.Cancelled, "user-1");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("reason is required");
    }

    [Fact]
    public void TransitionTo_Cancelled_WithReason_Succeeds()
    {
        var competition = CreateDraftCompetition();

        var result = competition.TransitionTo(
            CompetitionStatus.Cancelled, "user-1", "Budget constraints");

        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.Cancelled);
        competition.StatusChangeReason.Should().Be("Budget constraints");
    }

    [Fact]
    public void TransitionTo_Rejected_RequiresReason()
    {
        var competition = CreateDraftCompetition();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");
        competition.TransitionTo(CompetitionStatus.PendingApproval, "user-1");

        var result = competition.TransitionTo(CompetitionStatus.Rejected, "user-1");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("reason is required");
    }

    [Fact]
    public void TransitionTo_Suspended_RequiresReason()
    {
        var competition = CreateDraftCompetition();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");
        competition.TransitionTo(CompetitionStatus.PendingApproval, "user-1");
        competition.TransitionTo(CompetitionStatus.Approved, "user-1");
        competition.TransitionTo(CompetitionStatus.Published, "user-1");

        var result = competition.TransitionTo(CompetitionStatus.Suspended, "user-1");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("reason is required");
    }

    [Fact]
    public void TransitionTo_Suspended_RecordsPreviousStatus()
    {
        var competition = CreateDraftCompetition();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");
        competition.TransitionTo(CompetitionStatus.PendingApproval, "user-1");
        competition.TransitionTo(CompetitionStatus.Approved, "user-1");
        competition.TransitionTo(CompetitionStatus.Published, "user-1");

        var result = competition.TransitionTo(
            CompetitionStatus.Suspended, "user-1", "Pending legal review");

        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.Suspended);
        competition.SuspendedFromStatus.Should().Be(CompetitionStatus.Published);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Resume from Suspension
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Resume_FromSuspended_RestoresPreviousStatus()
    {
        var competition = CreateDraftCompetition();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");
        competition.TransitionTo(CompetitionStatus.PendingApproval, "user-1");
        competition.TransitionTo(CompetitionStatus.Approved, "user-1");
        competition.TransitionTo(CompetitionStatus.Published, "user-1");
        competition.TransitionTo(CompetitionStatus.Suspended, "user-1", "Legal review");

        var result = competition.Resume("user-1");

        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.Published);
        competition.SuspendedFromStatus.Should().BeNull();
    }

    [Fact]
    public void Resume_WhenNotSuspended_Fails()
    {
        var competition = CreateDraftCompetition();

        var result = competition.Resume("user-1");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not suspended");
    }

    // ═══════════════════════════════════════════════════════════════
    //  Rejected → UnderPreparation (Rework)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionTo_RejectedToUnderPreparation_Succeeds()
    {
        var competition = CreateDraftCompetition();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");
        competition.TransitionTo(CompetitionStatus.PendingApproval, "user-1");
        competition.TransitionTo(CompetitionStatus.Rejected, "approver-1", "Incomplete sections");

        var result = competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");

        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.UnderPreparation);
        competition.CurrentPhase.Should().Be(CompetitionPhase.BookletPreparation);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Terminal States — No Further Transitions
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionTo_FromContractSigned_Fails()
    {
        var competition = CreateDraftCompetition();
        // Walk through full lifecycle
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");
        competition.TransitionTo(CompetitionStatus.PendingApproval, "user-1");
        competition.TransitionTo(CompetitionStatus.Approved, "user-1");
        competition.TransitionTo(CompetitionStatus.Published, "user-1");
        competition.TransitionTo(CompetitionStatus.InquiryPeriod, "user-1");
        competition.TransitionTo(CompetitionStatus.ReceivingOffers, "user-1");
        competition.TransitionTo(CompetitionStatus.OffersClosed, "user-1");
        competition.TransitionTo(CompetitionStatus.TechnicalAnalysis, "user-1");
        competition.TransitionTo(CompetitionStatus.TechnicalAnalysisCompleted, "user-1");
        competition.TransitionTo(CompetitionStatus.FinancialAnalysis, "user-1");
        competition.TransitionTo(CompetitionStatus.FinancialAnalysisCompleted, "user-1");
        competition.TransitionTo(CompetitionStatus.AwardNotification, "user-1");
        competition.TransitionTo(CompetitionStatus.AwardApproved, "user-1");
        competition.TransitionTo(CompetitionStatus.ContractApproval, "user-1");
        competition.TransitionTo(CompetitionStatus.ContractApproved, "user-1");
        competition.TransitionTo(CompetitionStatus.ContractSigned, "user-1");

        competition.IsTerminal.Should().BeTrue();

        var result = competition.TransitionTo(CompetitionStatus.Draft, "user-1");
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void TransitionTo_FromCancelled_Fails()
    {
        var competition = CreateDraftCompetition();
        competition.TransitionTo(CompetitionStatus.Cancelled, "user-1", "No longer needed");

        competition.IsTerminal.Should().BeTrue();

        var result = competition.TransitionTo(CompetitionStatus.Draft, "user-1");
        result.IsFailure.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Domain Events
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionTo_RaisesDomainEvent()
    {
        var competition = CreateDraftCompetition();
        competition.ClearDomainEvents(); // Clear creation event

        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");

        competition.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CompetitionStatusChangedEvent>()
            .Which.Should().Match<CompetitionStatusChangedEvent>(e =>
                e.PreviousStatus == CompetitionStatus.Draft &&
                e.NewStatus == CompetitionStatus.UnderPreparation &&
                e.ChangedByUserId == "user-1");
    }

    // ═══════════════════════════════════════════════════════════════
    //  Legacy Convenience Methods
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void SubmitForApproval_WithoutSections_Fails()
    {
        var competition = CreateDraftCompetition();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");

        var result = competition.SubmitForApproval("user-1");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least one section");
    }

    [Fact]
    public void SubmitForApproval_WithSections_Succeeds()
    {
        var competition = CreateCompetitionWithSections();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");

        var result = competition.SubmitForApproval("user-1");

        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.PendingApproval);
    }

    [Fact]
    public void Approve_FromPendingApproval_Succeeds()
    {
        var competition = CreateCompetitionWithSections();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");
        competition.SubmitForApproval("user-1");

        var result = competition.Approve("approver-1");

        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.Approved);
    }

    [Fact]
    public void Reject_FromPendingApproval_Succeeds()
    {
        var competition = CreateCompetitionWithSections();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");
        competition.SubmitForApproval("user-1");

        var result = competition.Reject("approver-1", "Missing criteria");

        result.IsSuccess.Should().BeTrue();
        competition.Status.Should().Be(CompetitionStatus.Rejected);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Query Helpers
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GetAllowedTransitions_FromDraft_ReturnsCorrectSet()
    {
        var competition = CreateDraftCompetition();

        var allowed = competition.GetAllowedTransitions();

        allowed.Should().Contain(CompetitionStatus.UnderPreparation);
        allowed.Should().Contain(CompetitionStatus.Cancelled);
        allowed.Should().NotContain(CompetitionStatus.Published);
    }

    [Fact]
    public void IsEditable_InDraft_ReturnsTrue()
    {
        var competition = CreateDraftCompetition();
        competition.IsEditable.Should().BeTrue();
    }

    [Fact]
    public void IsEditable_InApproved_ReturnsFalse()
    {
        var competition = CreateDraftCompetition();
        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");
        competition.TransitionTo(CompetitionStatus.PendingApproval, "user-1");
        competition.TransitionTo(CompetitionStatus.Approved, "user-1");

        competition.IsEditable.Should().BeFalse();
    }

    [Fact]
    public void PhaseNumber_InDraft_Returns1()
    {
        var competition = CreateDraftCompetition();
        competition.PhaseNumber.Should().Be(1);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Version Increment
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionTo_IncrementsVersion()
    {
        var competition = CreateDraftCompetition();
        var initialVersion = competition.Version;

        competition.TransitionTo(CompetitionStatus.UnderPreparation, "user-1");

        competition.Version.Should().Be(initialVersion + 1);
    }
}
