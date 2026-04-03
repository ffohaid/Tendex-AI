using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Represents a role within the platform's RBAC system.
/// Roles define a set of permissions and can be assigned to users within a tenant.
/// 
/// GOVERNANCE MODEL:
///   - Protected roles (IsProtected = true) are immutable governance roles (Tier 1 & 2).
///     They cannot be edited, deleted, deactivated, or have their permissions modified.
///   - Flexible roles (IsProtected = false) are Tier 3 roles that can be fully
///     customized per tenant via the Permission Matrix.
/// </summary>
public sealed class Role : AggregateRoot<Guid>
{
    private Role() { } // EF Core parameterless constructor

    public Role(
        string nameAr,
        string nameEn,
        string normalizedName,
        Guid tenantId,
        bool isSystemRole = false,
        bool isProtected = false)
    {
        Id = Guid.NewGuid();
        NameAr = nameAr;
        NameEn = nameEn;
        NormalizedName = normalizedName.ToUpperInvariant();
        TenantId = tenantId;
        IsSystemRole = isSystemRole;
        IsProtected = isProtected;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Arabic name of the role.</summary>
    public string NameAr { get; private set; } = null!;

    /// <summary>English name of the role.</summary>
    public string NameEn { get; private set; } = null!;

    /// <summary>Normalized role name for case-insensitive lookups.</summary>
    public string NormalizedName { get; private set; } = null!;

    /// <summary>Optional description of the role's purpose.</summary>
    public string? Description { get; private set; }

    /// <summary>The tenant this role belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Whether this is a system-defined role (seeded by default).</summary>
    public bool IsSystemRole { get; private set; }

    /// <summary>
    /// Whether this role is a protected governance role (Tier 1 or Tier 2).
    /// Protected roles CANNOT be edited, deleted, deactivated, or have their permissions modified.
    /// This includes: OperatorSuperAdmin and TenantPrimaryAdmin.
    /// </summary>
    public bool IsProtected { get; private set; }

    /// <summary>Whether this role is currently active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Concurrency stamp for optimistic concurrency control.</summary>
    public string ConcurrencyStamp { get; private set; } = Guid.NewGuid().ToString("N");

    /// <summary>Navigation property: users assigned to this role.</summary>
    public ICollection<UserRole> UserRoles { get; private set; } = [];

    /// <summary>Navigation property: permissions assigned to this role.</summary>
    public ICollection<RolePermission> RolePermissions { get; private set; } = [];

    // ----- Domain Methods with Governance Guards -----

    /// <summary>
    /// Updates the role name. Throws if the role is protected.
    /// </summary>
    public void UpdateName(string nameAr, string nameEn)
    {
        EnsureNotProtected("update");
        NameAr = nameAr;
        NameEn = nameEn;
        NormalizedName = nameEn.ToUpperInvariant();
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the role description. Throws if the role is protected.
    /// </summary>
    public void SetDescription(string? description)
    {
        EnsureNotProtected("modify description of");
        Description = description;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the role. Protected roles are always active.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the role. Throws if the role is protected.
    /// </summary>
    public void Deactivate()
    {
        EnsureNotProtected("deactivate");
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Ensures the role is not protected before allowing modification.
    /// </summary>
    private void EnsureNotProtected(string action)
    {
        if (IsProtected)
        {
            throw new InvalidOperationException(
                $"Cannot {action} a protected governance role ('{NameEn}'). " +
                "Protected roles are immutable and managed by the system.");
        }
    }
}
