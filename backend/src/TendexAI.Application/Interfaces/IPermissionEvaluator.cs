using TendexAI.Domain.Enums;

namespace TendexAI.Application.Interfaces;

/// <summary>
/// Central permission evaluation engine that checks if a user has permission
/// to perform a specific action on a specific resource.
/// This is the single source of truth for all permission checks in the system.
/// </summary>
public interface IPermissionEvaluator
{
    /// <summary>
    /// Evaluates whether the current user has permission to perform the specified action
    /// on the specified resource type within the given scope.
    /// </summary>
    /// <param name="userId">The ID of the user to check permissions for.</param>
    /// <param name="scope">The resource scope (Global, Competition, Committee).</param>
    /// <param name="resourceType">The type of resource being accessed.</param>
    /// <param name="action">The action being performed.</param>
    /// <param name="competitionId">Optional: The competition ID (for Competition scope).</param>
    /// <param name="committeeId">Optional: The committee ID (for Committee scope).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has permission, false otherwise.</returns>
    Task<bool> HasPermissionAsync(
        string userId,
        ResourceScope scope,
        ResourceType resourceType,
        PermissionAction action,
        Guid? competitionId = null,
        Guid? committeeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all allowed actions for a user on a specific resource type.
    /// </summary>
    Task<PermissionAction> GetAllowedActionsAsync(
        string userId,
        ResourceScope scope,
        ResourceType resourceType,
        Guid? competitionId = null,
        Guid? committeeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a complete permission summary for a user across all resource types.
    /// Used for building the user's permission profile in the frontend.
    /// </summary>
    Task<IReadOnlyList<PermissionSummaryItem>> GetUserPermissionSummaryAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a complete permission summary for a user within a specific competition.
    /// Includes phase-aware permissions.
    /// </summary>
    Task<IReadOnlyList<CompetitionPermissionSummaryItem>> GetUserCompetitionPermissionSummaryAsync(
        string userId,
        Guid competitionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a single permission summary item for a user.
/// </summary>
public record PermissionSummaryItem(
    ResourceScope Scope,
    ResourceType ResourceType,
    string ResourceTypeNameAr,
    string ResourceTypeNameEn,
    PermissionAction AllowedActions);

/// <summary>
/// Represents a permission summary item for a user within a competition context.
/// </summary>
public record CompetitionPermissionSummaryItem(
    ResourceType ResourceType,
    string ResourceTypeNameAr,
    string ResourceTypeNameEn,
    CompetitionPhase Phase,
    string PhaseNameAr,
    string PhaseNameEn,
    CommitteeRole CommitteeRole,
    PermissionAction AllowedActions);
