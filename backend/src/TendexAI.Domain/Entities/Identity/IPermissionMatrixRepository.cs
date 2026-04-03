using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Repository interface for the flexible permission matrix rules.
/// </summary>
public interface IPermissionMatrixRepository
{
    /// <summary>
    /// Gets all active rules for a specific role, resource scope, and resource type.
    /// Optionally filters by committee role and competition phase.
    /// </summary>
    Task<IReadOnlyList<PermissionMatrixRule>> GetRulesAsync(
        Guid roleId,
        ResourceScope scope,
        ResourceType resourceType,
        CommitteeRole? committeeRole = null,
        CompetitionPhase? competitionPhase = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active rules for a specific role (across all scopes and resource types).
    /// Used for building the full permission summary for a user.
    /// </summary>
    Task<IReadOnlyList<PermissionMatrixRule>> GetAllRulesForRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all rules for a specific tenant (for the matrix management UI).
    /// </summary>
    Task<IReadOnlyList<PermissionMatrixRule>> GetAllByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific rule by its dimensions.
    /// </summary>
    Task<PermissionMatrixRule?> GetRuleAsync(
        Guid roleId,
        ResourceScope scope,
        ResourceType resourceType,
        CommitteeRole? committeeRole = null,
        CompetitionPhase? competitionPhase = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a rule by its ID.
    /// </summary>
    Task<PermissionMatrixRule?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new rule.
    /// </summary>
    Task AddAsync(
        PermissionMatrixRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple rules at once (for seeding or bulk operations).
    /// </summary>
    Task AddRangeAsync(
        IEnumerable<PermissionMatrixRule> rules,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all rules for a specific tenant (for reset operations).
    /// </summary>
    Task RemoveAllByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
