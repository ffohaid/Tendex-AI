using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.StateMachine;

/// <summary>
/// Domain service interface for evaluating competition permissions
/// against the 4-dimensional permissions matrix.
/// 
/// Dimensions:
///   1. Competition (RFP) — which competition
///   2. Phase — which phase the competition is in
///   3. Committee Role — user's role within the committee for this competition
///   4. System Role — user's system-level role
/// </summary>
public interface ICompetitionPermissionService
{
    /// <summary>
    /// Checks if a user has a specific action permission for a competition
    /// in its current phase.
    /// </summary>
    /// <param name="competitionId">The competition ID (Dimension 1).</param>
    /// <param name="phase">The current competition phase (Dimension 2).</param>
    /// <param name="userId">The user to check permissions for.</param>
    /// <param name="action">The action to check.</param>
    /// <param name="resourceType">The resource type (default: "Competition").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has the specified permission.</returns>
    Task<bool> HasPermissionAsync(
        Guid competitionId,
        CompetitionPhase phase,
        string userId,
        PermissionAction action,
        string resourceType = "Competition",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all allowed actions for a user on a specific competition in its current phase.
    /// </summary>
    Task<PermissionAction> GetAllowedActionsAsync(
        Guid competitionId,
        CompetitionPhase phase,
        string userId,
        string resourceType = "Competition",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a user can perform a status transition on a competition.
    /// Combines permission check with state machine validation.
    /// </summary>
    Task<Result> ValidateTransitionPermissionAsync(
        Guid competitionId,
        CompetitionStatus currentStatus,
        CompetitionStatus targetStatus,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective permissions for a user across all phases of a competition.
    /// Used for displaying the permissions overview in the UI.
    /// </summary>
    Task<IReadOnlyList<PhasePermissionSummary>> GetUserPermissionSummaryAsync(
        Guid competitionId,
        string userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Summary of a user's permissions for a specific phase.
/// </summary>
public sealed record PhasePermissionSummary(
    CompetitionPhase Phase,
    string PhaseNameAr,
    string PhaseNameEn,
    CommitteeRole CommitteeRole,
    SystemRole SystemRole,
    PermissionAction AllowedActions,
    bool IsCurrentPhase);
