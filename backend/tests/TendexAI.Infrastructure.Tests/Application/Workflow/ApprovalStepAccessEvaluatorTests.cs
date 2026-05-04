using TendexAI.Application.Features.Workflow.Services;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Tests.Features.Workflow;

public class ApprovalStepAccessEvaluatorTests
{
    [Fact]
    public void CanCurrentUserAct_ReturnsTrue_ForCurrentPendingStep_WhenRoleMatches()
    {
        var step = CreateStep(stepOrder: 1, requiredRole: SystemRole.TenantPrimaryAdmin);

        var canAct = ApprovalStepAccessEvaluator.CanCurrentUserAct(
            step,
            currentStepOrder: 1,
            isWorkflowCompleted: false,
            isWorkflowRejected: false,
            userSystemRoles: new HashSet<SystemRole> { SystemRole.TenantPrimaryAdmin },
            userCommitteeRoles: new HashSet<CommitteeRole>());

        Assert.True(canAct);
    }

    [Fact]
    public void CanCurrentUserAct_ReturnsFalse_ForFutureStep_EvenWhenRoleMatches()
    {
        var step = CreateStep(stepOrder: 2, requiredRole: SystemRole.TenantPrimaryAdmin);

        var canAct = ApprovalStepAccessEvaluator.CanCurrentUserAct(
            step,
            currentStepOrder: 1,
            isWorkflowCompleted: false,
            isWorkflowRejected: false,
            userSystemRoles: new HashSet<SystemRole> { SystemRole.TenantPrimaryAdmin },
            userCommitteeRoles: new HashSet<CommitteeRole>());

        Assert.False(canAct);
    }

    [Fact]
    public void CanCurrentUserAct_ReturnsFalse_WhenRoleDoesNotMatchCurrentStep()
    {
        var step = CreateStep(stepOrder: 1, requiredRole: SystemRole.FinancialController);

        var canAct = ApprovalStepAccessEvaluator.CanCurrentUserAct(
            step,
            currentStepOrder: 1,
            isWorkflowCompleted: false,
            isWorkflowRejected: false,
            userSystemRoles: new HashSet<SystemRole> { SystemRole.TenantPrimaryAdmin },
            userCommitteeRoles: new HashSet<CommitteeRole>());

        Assert.False(canAct);
    }

    [Fact]
    public void CanCurrentUserAct_ReturnsFalse_WhenCommitteeRoleIsRequiredButMissing()
    {
        var step = CreateStep(
            stepOrder: 1,
            requiredRole: SystemRole.Member,
            requiredCommitteeRole: CommitteeRole.PreparationCommitteeChair);

        var canAct = ApprovalStepAccessEvaluator.CanCurrentUserAct(
            step,
            currentStepOrder: 1,
            isWorkflowCompleted: false,
            isWorkflowRejected: false,
            userSystemRoles: new HashSet<SystemRole> { SystemRole.Member },
            userCommitteeRoles: new HashSet<CommitteeRole>());

        Assert.False(canAct);
    }

    [Fact]
    public void CanCurrentUserAct_ReturnsTrue_WhenCommitteeRoleMatches()
    {
        var step = CreateStep(
            stepOrder: 1,
            requiredRole: SystemRole.Member,
            requiredCommitteeRole: CommitteeRole.PreparationCommitteeChair);

        var canAct = ApprovalStepAccessEvaluator.CanCurrentUserAct(
            step,
            currentStepOrder: 1,
            isWorkflowCompleted: false,
            isWorkflowRejected: false,
            userSystemRoles: new HashSet<SystemRole> { SystemRole.Member },
            userCommitteeRoles: new HashSet<CommitteeRole> { CommitteeRole.PreparationCommitteeChair });

        Assert.True(canAct);
    }

    private static ApprovalWorkflowStep CreateStep(
        int stepOrder,
        SystemRole requiredRole,
        CommitteeRole requiredCommitteeRole = CommitteeRole.None)
    {
        return ApprovalWorkflowStep.Create(
            competitionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            fromStatus: CompetitionStatus.PendingApproval,
            toStatus: CompetitionStatus.Approved,
            stepOrder: stepOrder,
            requiredRole: requiredRole,
            requiredCommitteeRole: requiredCommitteeRole,
            stepNameAr: "Test Step",
            stepNameEn: "Test Step",
            createdBy: "test-user");
    }
}
