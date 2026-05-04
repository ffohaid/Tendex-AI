using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Workflow.Services;

/// <summary>
/// Evaluates whether the current authenticated user can act on a specific approval step.
/// Keeps UI-facing action eligibility aligned with backend authorization semantics.
/// </summary>
public static class ApprovalStepAccessEvaluator
{
    public static bool CanCurrentUserAct(
        ApprovalWorkflowStep step,
        int currentStepOrder,
        bool isWorkflowCompleted,
        bool isWorkflowRejected,
        ISet<SystemRole> userSystemRoles,
        ISet<CommitteeRole> userCommitteeRoles)
    {
        if (isWorkflowCompleted || isWorkflowRejected)
            return false;

        if (step.StepOrder != currentStepOrder)
            return false;

        if (!step.IsActionable)
            return false;

        if (!userSystemRoles.Contains(step.RequiredRole))
            return false;

        if (step.RequiredCommitteeRole == CommitteeRole.None)
            return true;

        return userCommitteeRoles.Contains(step.RequiredCommitteeRole);
    }
}
