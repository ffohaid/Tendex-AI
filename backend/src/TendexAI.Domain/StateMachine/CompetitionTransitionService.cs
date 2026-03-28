using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.StateMachine;

/// <summary>
/// Domain service that orchestrates competition status transitions.
/// Combines state machine validation with prerequisite checks to ensure
/// all business rules are satisfied before allowing a transition.
/// </summary>
public sealed class CompetitionTransitionService
{
    /// <summary>
    /// Attempts to transition a competition to a new status.
    /// Validates the state machine rules and all prerequisites.
    /// </summary>
    /// <param name="currentStatus">The current status of the competition.</param>
    /// <param name="targetStatus">The desired target status.</param>
    /// <param name="context">The prerequisite evaluation context.</param>
    /// <returns>A result indicating success or failure with detailed error messages.</returns>
    public static Result ValidateTransition(
        CompetitionStatus currentStatus,
        CompetitionStatus targetStatus,
        IPhasePrerequisiteContext context)
    {
        // Step 1: Validate state machine allows this transition
        var stateMachineResult = CompetitionStateMachine.ValidateTransition(currentStatus, targetStatus);
        if (stateMachineResult.IsFailure)
            return stateMachineResult;

        // Step 2: Validate all prerequisites for the target status
        var prerequisiteResult = PhasePrerequisiteRegistry.ValidatePrerequisites(targetStatus, context);
        if (prerequisiteResult.IsFailure)
            return prerequisiteResult;

        return Result.Success();
    }

    /// <summary>
    /// Returns detailed information about the transition feasibility,
    /// including individual prerequisite check results.
    /// </summary>
    public static TransitionValidationResult GetTransitionDetails(
        CompetitionStatus currentStatus,
        CompetitionStatus targetStatus,
        IPhasePrerequisiteContext context)
    {
        var canTransition = CompetitionStateMachine.CanTransition(currentStatus, targetStatus);
        var prerequisites = PhasePrerequisiteRegistry.CheckPrerequisites(targetStatus, context);
        var allPrerequisitesMet = prerequisites.All(p => p.IsSatisfied);

        return new TransitionValidationResult(
            IsAllowed: canTransition && allPrerequisitesMet,
            IsStateMachineValid: canTransition,
            ArePrerequisitesMet: allPrerequisitesMet,
            CurrentStatus: currentStatus,
            TargetStatus: targetStatus,
            CurrentPhase: CompetitionStateMachine.GetPhase(currentStatus),
            TargetPhase: CompetitionStateMachine.GetPhase(targetStatus),
            Prerequisites: prerequisites,
            AllowedTransitions: CompetitionStateMachine.GetAllowedTransitions(currentStatus)
                .Select(s => new AllowedTransitionInfo(
                    s,
                    CompetitionStateMachine.GetPhase(s),
                    CompetitionStateMachine.GetPhaseNameAr(CompetitionStateMachine.GetPhase(s)),
                    CompetitionStateMachine.GetPhaseNameEn(CompetitionStateMachine.GetPhase(s))))
                .ToList()
                .AsReadOnly());
    }
}

/// <summary>
/// Detailed result of a transition validation, including prerequisite details.
/// </summary>
public sealed record TransitionValidationResult(
    bool IsAllowed,
    bool IsStateMachineValid,
    bool ArePrerequisitesMet,
    CompetitionStatus CurrentStatus,
    CompetitionStatus TargetStatus,
    CompetitionPhase CurrentPhase,
    CompetitionPhase TargetPhase,
    IReadOnlyList<PrerequisiteCheckResult> Prerequisites,
    IReadOnlyList<AllowedTransitionInfo> AllowedTransitions);

/// <summary>
/// Information about an allowed transition target.
/// </summary>
public sealed record AllowedTransitionInfo(
    CompetitionStatus Status,
    CompetitionPhase Phase,
    string PhaseNameAr,
    string PhaseNameEn);
