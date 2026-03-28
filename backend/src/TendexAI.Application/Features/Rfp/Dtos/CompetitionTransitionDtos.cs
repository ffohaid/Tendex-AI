using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Dtos;

/// <summary>
/// Result of a competition status transition.
/// </summary>
public sealed record CompetitionTransitionResultDto(
    Guid CompetitionId,
    CompetitionStatus PreviousStatus,
    CompetitionStatus NewStatus,
    CompetitionPhase PreviousPhase,
    CompetitionPhase NewPhase,
    string PhaseNameAr,
    string PhaseNameEn,
    DateTime TransitionedAt,
    IReadOnlyList<AllowedTransitionDto> AllowedNextTransitions);

/// <summary>
/// Represents an allowed transition from the current status.
/// </summary>
public sealed record AllowedTransitionDto(
    CompetitionStatus Status,
    string StatusName,
    CompetitionPhase Phase,
    string PhaseNameAr,
    string PhaseNameEn);

/// <summary>
/// Detailed prerequisite check result for a target transition.
/// </summary>
public sealed record PrerequisiteCheckDto(
    string Code,
    string DescriptionAr,
    string DescriptionEn,
    bool IsSatisfied);

/// <summary>
/// Full transition feasibility report.
/// </summary>
public sealed record TransitionFeasibilityDto(
    bool IsAllowed,
    bool IsStateMachineValid,
    bool ArePrerequisitesMet,
    CompetitionStatus CurrentStatus,
    CompetitionStatus TargetStatus,
    CompetitionPhase CurrentPhase,
    CompetitionPhase TargetPhase,
    IReadOnlyList<PrerequisiteCheckDto> Prerequisites,
    IReadOnlyList<AllowedTransitionDto> AllowedTransitions);

/// <summary>
/// Summary of a user's permissions for a specific phase.
/// </summary>
public sealed record PhasePermissionDto(
    CompetitionPhase Phase,
    string PhaseNameAr,
    string PhaseNameEn,
    CommitteeRole CommitteeRole,
    SystemRole SystemRole,
    PermissionAction AllowedActions,
    bool IsCurrentPhase);
