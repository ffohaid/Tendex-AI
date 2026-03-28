using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Represents a single entry in the 4-dimensional permissions matrix.
/// Each entry defines what actions a specific combination of
/// (Competition Phase × Committee Role × System Role) can perform.
/// 
/// The four dimensions are:
///   1. Competition (RFP) — resolved at query time via CompetitionId
///   2. Phase — the competition phase this permission applies to
///   3. Committee Role — the user's role within a committee
///   4. System Role — the user's system-level role
///   
/// PRD Reference: Section 5 — مصفوفة الصلاحيات الديناميكية
/// </summary>
public sealed class CompetitionPermissionMatrix : BaseEntity<Guid>
{
    private CompetitionPermissionMatrix() { } // EF Core constructor

    /// <summary>
    /// Creates a new permission matrix entry.
    /// </summary>
    public static CompetitionPermissionMatrix Create(
        Guid tenantId,
        CompetitionPhase phase,
        CommitteeRole committeeRole,
        SystemRole systemRole,
        PermissionAction allowedActions,
        string? resourceType = null,
        string createdBy = "system")
    {
        return new CompetitionPermissionMatrix
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Phase = phase,
            CommitteeRole = committeeRole,
            SystemRole = systemRole,
            AllowedActions = allowedActions,
            ResourceType = resourceType ?? "Competition",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>Tenant this permission matrix entry belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Dimension 2: The competition phase this permission applies to.</summary>
    public CompetitionPhase Phase { get; private set; }

    /// <summary>Dimension 3: The committee role this permission applies to.</summary>
    public CommitteeRole CommitteeRole { get; private set; }

    /// <summary>Dimension 4: The system-level role this permission applies to.</summary>
    public SystemRole SystemRole { get; private set; }

    /// <summary>The set of actions allowed for this combination (flags enum).</summary>
    public PermissionAction AllowedActions { get; private set; }

    /// <summary>
    /// The resource type this permission applies to (e.g., "Competition", "Section", "BoqItem").
    /// Allows fine-grained control over different entity types.
    /// </summary>
    public string ResourceType { get; private set; } = "Competition";

    /// <summary>Whether this permission entry is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Checks if a specific action is allowed by this matrix entry.
    /// </summary>
    public bool HasAction(PermissionAction action)
    {
        return (AllowedActions & action) == action;
    }

    /// <summary>
    /// Updates the allowed actions for this matrix entry.
    /// </summary>
    public void UpdateAllowedActions(PermissionAction newActions, string modifiedBy)
    {
        AllowedActions = newActions;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Activates or deactivates this permission entry.
    /// </summary>
    public void SetActive(bool isActive, string modifiedBy)
    {
        IsActive = isActive;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
