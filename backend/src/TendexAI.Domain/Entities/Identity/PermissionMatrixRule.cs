using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Represents a single rule in the flexible N-dimensional permission matrix.
/// Each rule defines what actions a specific combination of
/// (ResourceScope × ResourceType × SystemRole × CommitteeRole × CompetitionPhase)
/// can perform.
///
/// The dimensions are:
///   1. ResourceScope — Global, Competition, or Committee
///   2. ResourceType — The specific resource being controlled
///   3. SystemRole — The user's system-level role (linked to Role entity)
///   4. CommitteeRole — The user's role within a committee (optional, for Competition/Committee scope)
///   5. CompetitionPhase — The competition phase (optional, for Competition scope only)
///
/// Each tenant has its own copy of the matrix that can be customized by the tenant admin.
/// PRD Reference: Section 5 — مصفوفة الصلاحيات الديناميكية
/// </summary>
public sealed class PermissionMatrixRule : BaseEntity<Guid>
{
    private PermissionMatrixRule() { } // EF Core constructor

    /// <summary>
    /// Creates a new permission matrix rule.
    /// </summary>
    public static PermissionMatrixRule Create(
        Guid tenantId,
        Guid roleId,
        ResourceScope scope,
        ResourceType resourceType,
        PermissionAction allowedActions,
        CommitteeRole? committeeRole = null,
        CompetitionPhase? competitionPhase = null,
        bool isCustomized = false,
        string createdBy = "system")
    {
        return new PermissionMatrixRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RoleId = roleId,
            Scope = scope,
            ResourceType = resourceType,
            AllowedActions = allowedActions,
            CommitteeRole = committeeRole,
            CompetitionPhase = competitionPhase,
            IsCustomized = isCustomized,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>Tenant this rule belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The role this rule applies to (references identity.Roles).</summary>
    public Guid RoleId { get; private set; }

    /// <summary>Navigation property to the Role entity.</summary>
    public Role? Role { get; private set; }

    /// <summary>Dimension 1: The scope of the resource.</summary>
    public ResourceScope Scope { get; private set; }

    /// <summary>Dimension 2: The type of resource being controlled.</summary>
    public ResourceType ResourceType { get; private set; }

    /// <summary>
    /// Dimension 3 (optional): The committee role this rule applies to.
    /// Only relevant when Scope is Competition or Committee.
    /// Null means the rule applies regardless of committee role.
    /// </summary>
    public CommitteeRole? CommitteeRole { get; private set; }

    /// <summary>
    /// Dimension 4 (optional): The competition phase this rule applies to.
    /// Only relevant when Scope is Competition.
    /// Null means the rule applies to all phases.
    /// </summary>
    public CompetitionPhase? CompetitionPhase { get; private set; }

    /// <summary>The set of actions allowed for this combination (flags enum).</summary>
    public PermissionAction AllowedActions { get; private set; }

    /// <summary>Whether this rule has been customized by the tenant admin (vs. system default).</summary>
    public bool IsCustomized { get; private set; }

    /// <summary>Whether this rule is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Checks if a specific action is allowed by this rule.
    /// </summary>
    public bool HasAction(PermissionAction action)
    {
        return (AllowedActions & action) == action;
    }

    /// <summary>
    /// Updates the allowed actions for this rule.
    /// </summary>
    public void UpdateAllowedActions(PermissionAction newActions, string modifiedBy)
    {
        AllowedActions = newActions;
        IsCustomized = true;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Activates or deactivates this rule.
    /// </summary>
    public void SetActive(bool isActive, string modifiedBy)
    {
        IsActive = isActive;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Resets this rule to the default (non-customized) state.
    /// </summary>
    public void ResetToDefault(PermissionAction defaultActions, string modifiedBy)
    {
        AllowedActions = defaultActions;
        IsCustomized = false;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
