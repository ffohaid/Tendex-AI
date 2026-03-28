using FluentAssertions;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Domain.StateMachine;

namespace TendexAI.Infrastructure.Tests.Domain.Rfp;

/// <summary>
/// Unit tests for the 4D permission matrix, default seeder,
/// approval workflow templates, and phase prerequisites.
/// </summary>
public sealed class PermissionMatrixAndPrerequisiteTests
{
    // ═══════════════════════════════════════════════════════════════
    //  CompetitionPermissionMatrix Entity
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void PermissionMatrix_Create_SetsAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var entry = CompetitionPermissionMatrix.Create(
            tenantId,
            CompetitionPhase.BookletPreparation,
            CommitteeRole.PreparationCommitteeChair,
            SystemRole.Member,
            PermissionAction.Read | PermissionAction.Update | PermissionAction.Submit);

        entry.TenantId.Should().Be(tenantId);
        entry.Phase.Should().Be(CompetitionPhase.BookletPreparation);
        entry.CommitteeRole.Should().Be(CommitteeRole.PreparationCommitteeChair);
        entry.SystemRole.Should().Be(SystemRole.Member);
        entry.AllowedActions.Should().HaveFlag(PermissionAction.Read);
        entry.AllowedActions.Should().HaveFlag(PermissionAction.Update);
        entry.AllowedActions.Should().HaveFlag(PermissionAction.Submit);
        entry.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PermissionMatrix_HasAction_ChecksFlags()
    {
        var entry = CompetitionPermissionMatrix.Create(
            Guid.NewGuid(),
            CompetitionPhase.TechnicalAnalysis,
            CommitteeRole.TechnicalExamCommitteeChair,
            SystemRole.Member,
            PermissionAction.Read | PermissionAction.Score | PermissionAction.Approve);

        entry.HasAction(PermissionAction.Read).Should().BeTrue();
        entry.HasAction(PermissionAction.Score).Should().BeTrue();
        entry.HasAction(PermissionAction.Approve).Should().BeTrue();
        entry.HasAction(PermissionAction.Delete).Should().BeFalse();
        entry.HasAction(PermissionAction.Sign).Should().BeFalse();
    }

    [Fact]
    public void PermissionMatrix_UpdateAllowedActions_ChangesActions()
    {
        var entry = CompetitionPermissionMatrix.Create(
            Guid.NewGuid(),
            CompetitionPhase.BookletPreparation,
            CommitteeRole.None,
            SystemRole.Member,
            PermissionAction.Read);

        entry.UpdateAllowedActions(PermissionAction.Read | PermissionAction.Create, "admin-1");

        entry.AllowedActions.Should().HaveFlag(PermissionAction.Create);
        entry.LastModifiedBy.Should().Be("admin-1");
    }

    [Fact]
    public void PermissionMatrix_SetActive_DeactivatesEntry()
    {
        var entry = CompetitionPermissionMatrix.Create(
            Guid.NewGuid(),
            CompetitionPhase.BookletPreparation,
            CommitteeRole.None,
            SystemRole.Viewer,
            PermissionAction.Read);

        entry.SetActive(false, "admin-1");

        entry.IsActive.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════
    //  DefaultPermissionMatrixSeeder
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DefaultSeeder_GeneratesNonEmptyMatrix()
    {
        var tenantId = Guid.NewGuid();
        var entries = DefaultPermissionMatrixSeeder.GenerateDefaultMatrix(tenantId);

        entries.Should().NotBeEmpty();
        entries.Should().AllSatisfy(e => e.TenantId.Should().Be(tenantId));
    }

    [Fact]
    public void DefaultSeeder_CoversAllNinePhases()
    {
        var entries = DefaultPermissionMatrixSeeder.GenerateDefaultMatrix(Guid.NewGuid());
        var coveredPhases = entries.Select(e => e.Phase).Distinct().ToList();

        foreach (var phase in Enum.GetValues<CompetitionPhase>())
        {
            coveredPhases.Should().Contain(phase,
                $"Phase {phase} should be covered in the default matrix");
        }
    }

    [Fact]
    public void DefaultSeeder_ViewerRole_HasReadOnlyAcrossAllPhases()
    {
        var entries = DefaultPermissionMatrixSeeder.GenerateDefaultMatrix(Guid.NewGuid());
        var viewerEntries = entries.Where(e => e.SystemRole == SystemRole.Viewer).ToList();

        viewerEntries.Should().HaveCount(9); // One per phase
        viewerEntries.Should().AllSatisfy(e =>
        {
            e.AllowedActions.Should().Be(PermissionAction.Read);
        });
    }

    [Fact]
    public void DefaultSeeder_OwnerRole_HasApproveInBookletApproval()
    {
        var entries = DefaultPermissionMatrixSeeder.GenerateDefaultMatrix(Guid.NewGuid());
        var ownerApproval = entries.FirstOrDefault(e =>
            e.SystemRole == SystemRole.Owner &&
            e.Phase == CompetitionPhase.BookletApproval);

        ownerApproval.Should().NotBeNull();
        ownerApproval!.HasAction(PermissionAction.Approve).Should().BeTrue();
        ownerApproval.HasAction(PermissionAction.Reject).Should().BeTrue();
    }

    [Fact]
    public void DefaultSeeder_TechnicalChair_HasScoreAndApproveInTechnicalPhase()
    {
        var entries = DefaultPermissionMatrixSeeder.GenerateDefaultMatrix(Guid.NewGuid());
        var techChair = entries.FirstOrDefault(e =>
            e.CommitteeRole == CommitteeRole.TechnicalExamCommitteeChair &&
            e.Phase == CompetitionPhase.TechnicalAnalysis);

        techChair.Should().NotBeNull();
        techChair!.HasAction(PermissionAction.Score).Should().BeTrue();
        techChair.HasAction(PermissionAction.Approve).Should().BeTrue();
    }

    [Fact]
    public void DefaultSeeder_FinancialController_HasApproveInFinancialPhase()
    {
        var entries = DefaultPermissionMatrixSeeder.GenerateDefaultMatrix(Guid.NewGuid());
        var finCtrl = entries.FirstOrDefault(e =>
            e.SystemRole == SystemRole.FinancialController &&
            e.Phase == CompetitionPhase.FinancialAnalysis);

        finCtrl.Should().NotBeNull();
        finCtrl!.HasAction(PermissionAction.Approve).Should().BeTrue();
    }

    [Fact]
    public void DefaultSeeder_OwnerRole_HasSignInContractSigning()
    {
        var entries = DefaultPermissionMatrixSeeder.GenerateDefaultMatrix(Guid.NewGuid());
        var ownerSign = entries.FirstOrDefault(e =>
            e.SystemRole == SystemRole.Owner &&
            e.Phase == CompetitionPhase.ContractSigning);

        ownerSign.Should().NotBeNull();
        ownerSign!.HasAction(PermissionAction.Sign).Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════
    //  CompetitionCommitteeMember
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CommitteeMember_Create_SetsProperties()
    {
        var competitionId = Guid.NewGuid();
        var member = CompetitionCommitteeMember.Create(
            competitionId,
            Guid.NewGuid(),
            "user-1",
            CommitteeRole.TechnicalExamCommitteeChair,
            CompetitionPhase.TechnicalAnalysis,
            CompetitionPhase.TechnicalAnalysis);

        member.CompetitionId.Should().Be(competitionId);
        member.CommitteeRole.Should().Be(CommitteeRole.TechnicalExamCommitteeChair);
        member.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CommitteeMember_IsActiveForPhase_WithinRange_ReturnsTrue()
    {
        var member = CompetitionCommitteeMember.Create(
            Guid.NewGuid(), Guid.NewGuid(), "user-1",
            CommitteeRole.PreparationCommitteeChair,
            CompetitionPhase.BookletPreparation,
            CompetitionPhase.BookletApproval);

        member.IsActiveForPhase(CompetitionPhase.BookletPreparation).Should().BeTrue();
        member.IsActiveForPhase(CompetitionPhase.BookletApproval).Should().BeTrue();
    }

    [Fact]
    public void CommitteeMember_IsActiveForPhase_OutsideRange_ReturnsFalse()
    {
        var member = CompetitionCommitteeMember.Create(
            Guid.NewGuid(), Guid.NewGuid(), "user-1",
            CommitteeRole.TechnicalExamCommitteeChair,
            CompetitionPhase.TechnicalAnalysis,
            CompetitionPhase.TechnicalAnalysis);

        member.IsActiveForPhase(CompetitionPhase.BookletPreparation).Should().BeFalse();
        member.IsActiveForPhase(CompetitionPhase.FinancialAnalysis).Should().BeFalse();
    }

    [Fact]
    public void CommitteeMember_IsActiveForPhase_NoRange_AlwaysActive()
    {
        var member = CompetitionCommitteeMember.Create(
            Guid.NewGuid(), Guid.NewGuid(), "user-1",
            CommitteeRole.CommitteeSecretary);

        member.IsActiveForPhase(CompetitionPhase.BookletPreparation).Should().BeTrue();
        member.IsActiveForPhase(CompetitionPhase.ContractSigning).Should().BeTrue();
    }

    [Fact]
    public void CommitteeMember_Deactivate_SetsInactive()
    {
        var member = CompetitionCommitteeMember.Create(
            Guid.NewGuid(), Guid.NewGuid(), "user-1",
            CommitteeRole.PreparationCommitteeMember);

        member.Deactivate("admin-1", "Reassigned");

        member.IsActive.Should().BeFalse();
        member.RemovedBy.Should().Be("admin-1");
        member.RemovalReason.Should().Be("Reassigned");
    }

    [Fact]
    public void CommitteeMember_Reactivate_SetsActive()
    {
        var member = CompetitionCommitteeMember.Create(
            Guid.NewGuid(), Guid.NewGuid(), "user-1",
            CommitteeRole.PreparationCommitteeMember);
        member.Deactivate("admin-1", "Temp removal");

        member.Reactivate("admin-1");

        member.IsActive.Should().BeTrue();
        member.RemovedBy.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════
    //  ApprovalWorkflowStep
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ApprovalStep_Create_SetsProperties()
    {
        var step = ApprovalWorkflowStep.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            CompetitionStatus.PendingApproval,
            CompetitionStatus.Approved,
            1, SystemRole.FinancialController,
            CommitteeRole.None,
            "مراجعة المراقب المالي",
            "Financial Controller Review");

        step.StepOrder.Should().Be(1);
        step.RequiredRole.Should().Be(SystemRole.FinancialController);
        step.Status.Should().Be(ApprovalStepStatus.Pending);
    }

    [Fact]
    public void ApprovalStep_Approve_SetsApprovedStatus()
    {
        var step = ApprovalWorkflowStep.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            CompetitionStatus.PendingApproval,
            CompetitionStatus.Approved,
            1, SystemRole.Owner);

        var result = step.Approve("owner-1", "Looks good");

        result.IsSuccess.Should().BeTrue();
        step.Status.Should().Be(ApprovalStepStatus.Approved);
        step.CompletedByUserId.Should().Be("owner-1");
        step.Comment.Should().Be("Looks good");
    }

    [Fact]
    public void ApprovalStep_Reject_RequiresReason()
    {
        var step = ApprovalWorkflowStep.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            CompetitionStatus.PendingApproval,
            CompetitionStatus.Approved,
            1, SystemRole.Owner);

        var result = step.Reject("owner-1", "");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("reason is required");
    }

    [Fact]
    public void ApprovalStep_Reject_WithReason_Succeeds()
    {
        var step = ApprovalWorkflowStep.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            CompetitionStatus.PendingApproval,
            CompetitionStatus.Approved,
            1, SystemRole.Owner);

        var result = step.Reject("owner-1", "Incomplete documentation");

        result.IsSuccess.Should().BeTrue();
        step.Status.Should().Be(ApprovalStepStatus.Rejected);
    }

    [Fact]
    public void ApprovalStep_CannotApproveAlreadyApproved()
    {
        var step = ApprovalWorkflowStep.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            CompetitionStatus.PendingApproval,
            CompetitionStatus.Approved,
            1, SystemRole.Owner);
        step.Approve("owner-1");

        var result = step.Approve("owner-2");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ApprovalStep_Reset_ReturnsToPending()
    {
        var step = ApprovalWorkflowStep.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            CompetitionStatus.PendingApproval,
            CompetitionStatus.Approved,
            1, SystemRole.Owner);
        step.Approve("owner-1");

        step.Reset();

        step.Status.Should().Be(ApprovalStepStatus.Pending);
        step.CompletedByUserId.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════
    //  ApprovalWorkflowTemplate
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void WorkflowTemplate_BookletApproval_HasTwoSteps()
    {
        var templates = ApprovalWorkflowTemplate.GetStepTemplates(
            CompetitionStatus.PendingApproval, CompetitionStatus.Approved);

        templates.Should().HaveCount(2);
        templates[0].RequiredRole.Should().Be(SystemRole.FinancialController);
        templates[1].RequiredRole.Should().Be(SystemRole.Owner);
    }

    [Fact]
    public void WorkflowTemplate_TechnicalCompletion_HasOneStep()
    {
        var templates = ApprovalWorkflowTemplate.GetStepTemplates(
            CompetitionStatus.TechnicalAnalysis, CompetitionStatus.TechnicalAnalysisCompleted);

        templates.Should().HaveCount(1);
        templates[0].RequiredCommitteeRole.Should().Be(CommitteeRole.TechnicalExamCommitteeChair);
    }

    [Fact]
    public void WorkflowTemplate_FinancialCompletion_HasTwoSteps()
    {
        var templates = ApprovalWorkflowTemplate.GetStepTemplates(
            CompetitionStatus.FinancialAnalysis, CompetitionStatus.FinancialAnalysisCompleted);

        templates.Should().HaveCount(2);
        templates[0].RequiredCommitteeRole.Should().Be(CommitteeRole.FinancialExamCommitteeChair);
        templates[1].RequiredRole.Should().Be(SystemRole.FinancialController);
    }

    [Fact]
    public void WorkflowTemplate_ContractApproval_HasTwoSteps()
    {
        var templates = ApprovalWorkflowTemplate.GetStepTemplates(
            CompetitionStatus.ContractApproval, CompetitionStatus.ContractApproved);

        templates.Should().HaveCount(2);
        templates[0].RequiredRole.Should().Be(SystemRole.FinancialController);
        templates[1].RequiredRole.Should().Be(SystemRole.Owner);
    }

    [Fact]
    public void WorkflowTemplate_NoApprovalRequired_ReturnsEmpty()
    {
        var templates = ApprovalWorkflowTemplate.GetStepTemplates(
            CompetitionStatus.Approved, CompetitionStatus.Published);

        templates.Should().BeEmpty();
    }

    [Fact]
    public void WorkflowTemplate_RequiresApproval_ReturnsCorrectly()
    {
        ApprovalWorkflowTemplate.RequiresApproval(
            CompetitionStatus.PendingApproval, CompetitionStatus.Approved)
            .Should().BeTrue();

        ApprovalWorkflowTemplate.RequiresApproval(
            CompetitionStatus.Approved, CompetitionStatus.Published)
            .Should().BeFalse();
    }

    [Fact]
    public void WorkflowTemplate_CreateWorkflowSteps_CreatesConcreteSteps()
    {
        var competitionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var steps = ApprovalWorkflowTemplate.CreateWorkflowSteps(
            competitionId, tenantId,
            CompetitionStatus.PendingApproval, CompetitionStatus.Approved);

        steps.Should().HaveCount(2);
        steps.Should().AllSatisfy(s =>
        {
            s.CompetitionId.Should().Be(competitionId);
            s.TenantId.Should().Be(tenantId);
            s.Status.Should().Be(ApprovalStepStatus.Pending);
        });
    }

    // ═══════════════════════════════════════════════════════════════
    //  PhasePrerequisiteRegistry
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Prerequisites_PendingApproval_HasFourPrerequisites()
    {
        var prereqs = PhasePrerequisiteRegistry.GetPrerequisites(CompetitionStatus.PendingApproval);
        prereqs.Should().HaveCount(4);
    }

    [Fact]
    public void Prerequisites_Published_RequiresApprovalComplete()
    {
        var prereqs = PhasePrerequisiteRegistry.GetPrerequisites(CompetitionStatus.Published);
        prereqs.Should().ContainSingle();
        prereqs[0].Code.Should().Be("APPR_COMPLETE");
    }

    [Fact]
    public void Prerequisites_OffersClosed_RequiresDeadlineAndOffers()
    {
        var prereqs = PhasePrerequisiteRegistry.GetPrerequisites(CompetitionStatus.OffersClosed);
        prereqs.Should().HaveCount(2);
        prereqs.Select(p => p.Code).Should().Contain("OFFER_DEADLINE");
        prereqs.Select(p => p.Code).Should().Contain("OFFER_MIN");
    }

    [Fact]
    public void Prerequisites_ContractSigned_RequiresSignedCopy()
    {
        var prereqs = PhasePrerequisiteRegistry.GetPrerequisites(CompetitionStatus.ContractSigned);
        prereqs.Should().ContainSingle();
        prereqs[0].Code.Should().Be("SIGNED_COPY");
    }

    [Fact]
    public void Prerequisites_NoPrerequisites_ReturnsEmpty()
    {
        var prereqs = PhasePrerequisiteRegistry.GetPrerequisites(CompetitionStatus.Draft);
        prereqs.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePrerequisites_AllSatisfied_ReturnsSuccess()
    {
        var context = new TestPrerequisiteContext
        {
            AllMandatorySectionsCompleted = true,
            HasBoqItems = true,
            HasEvaluationCriteria = true,
            EvaluationWeightsValid = true
        };

        var result = PhasePrerequisiteRegistry.ValidatePrerequisites(
            CompetitionStatus.PendingApproval, context);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidatePrerequisites_SomeNotSatisfied_ReturnsFailure()
    {
        var context = new TestPrerequisiteContext
        {
            AllMandatorySectionsCompleted = true,
            HasBoqItems = false,
            HasEvaluationCriteria = true,
            EvaluationWeightsValid = true
        };

        var result = PhasePrerequisiteRegistry.ValidatePrerequisites(
            CompetitionStatus.PendingApproval, context);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("جدول الكميات");
    }

    [Fact]
    public void CheckPrerequisites_ReturnsDetailedResults()
    {
        var context = new TestPrerequisiteContext
        {
            AllMandatorySectionsCompleted = true,
            HasBoqItems = false,
            HasEvaluationCriteria = true,
            EvaluationWeightsValid = false
        };

        var results = PhasePrerequisiteRegistry.CheckPrerequisites(
            CompetitionStatus.PendingApproval, context);

        results.Should().HaveCount(4);
        results.Count(r => r.IsSatisfied).Should().Be(2);
        results.Count(r => !r.IsSatisfied).Should().Be(2);
    }

    // ═══════════════════════════════════════════════════════════════
    //  PhaseTransitionHistory
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionHistory_Create_SetsAllFields()
    {
        var history = PhaseTransitionHistory.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            CompetitionStatus.Draft, CompetitionStatus.UnderPreparation,
            CompetitionPhase.BookletPreparation, CompetitionPhase.BookletPreparation,
            "user-1", "Starting preparation");

        history.FromStatus.Should().Be(CompetitionStatus.Draft);
        history.ToStatus.Should().Be(CompetitionStatus.UnderPreparation);
        history.TransitionedByUserId.Should().Be("user-1");
        history.Reason.Should().Be("Starting preparation");
    }

    // ═══════════════════════════════════════════════════════════════
    //  CompetitionTransitionService
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionService_ValidTransitionWithPrerequisites_Succeeds()
    {
        var service = new CompetitionTransitionService();
        var context = new TestPrerequisiteContext
        {
            CurrentStatus = CompetitionStatus.UnderPreparation,
            AllMandatorySectionsCompleted = true,
            HasBoqItems = true,
            HasEvaluationCriteria = true,
            EvaluationWeightsValid = true
        };

        var result = CompetitionTransitionService.ValidateTransition(
            CompetitionStatus.UnderPreparation,
            CompetitionStatus.PendingApproval,
            context);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void TransitionService_InvalidStateMachineTransition_Fails()
    {
        var context = new TestPrerequisiteContext();

        var result = CompetitionTransitionService.ValidateTransition(
            CompetitionStatus.Draft,
            CompetitionStatus.Published,
            context);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void TransitionService_ValidTransitionFailedPrerequisites_Fails()
    {
        var context = new TestPrerequisiteContext
        {
            AllMandatorySectionsCompleted = false,
            HasBoqItems = false,
            HasEvaluationCriteria = false,
            EvaluationWeightsValid = false
        };

        var result = CompetitionTransitionService.ValidateTransition(
            CompetitionStatus.UnderPreparation,
            CompetitionStatus.PendingApproval,
            context);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void TransitionService_GetTransitionDetails_ReturnsFullInfo()
    {
        var context = new TestPrerequisiteContext
        {
            AllMandatorySectionsCompleted = true,
            HasBoqItems = true,
            HasEvaluationCriteria = true,
            EvaluationWeightsValid = false
        };

        var details = CompetitionTransitionService.GetTransitionDetails(
            CompetitionStatus.UnderPreparation,
            CompetitionStatus.PendingApproval,
            context);

        details.IsStateMachineValid.Should().BeTrue();
        details.ArePrerequisitesMet.Should().BeFalse();
        details.IsAllowed.Should().BeFalse();
        details.Prerequisites.Should().HaveCount(4);
        details.AllowedTransitions.Should().NotBeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Test Helper — Prerequisite Context
    // ═══════════════════════════════════════════════════════════════

    private sealed class TestPrerequisiteContext : IPhasePrerequisiteContext
    {
        public Guid CompetitionId { get; init; } = Guid.NewGuid();
        public CompetitionStatus CurrentStatus { get; init; } = CompetitionStatus.Draft;
        public bool AllMandatorySectionsCompleted { get; init; }
        public bool HasBoqItems { get; init; }
        public bool HasEvaluationCriteria { get; init; }
        public bool AllApprovalStepsCompleted { get; init; }
        public bool InquiryPeriodEnded { get; init; }
        public bool AllInquiriesAnswered { get; init; }
        public bool SubmissionDeadlinePassed { get; init; }
        public bool HasReceivedOffers { get; init; }
        public int OfferCount { get; init; }
        public bool TechnicalReportApproved { get; init; }
        public bool FinancialReportApproved { get; init; }
        public bool FinancialControllerApproved { get; init; }
        public bool AuthorityOwnerApproved { get; init; }
        public bool ContractDraftAttached { get; init; }
        public bool SignedContractAttached { get; init; }
        public decimal? EstimatedBudget { get; init; }
        public bool EvaluationWeightsValid { get; init; }
    }
}
