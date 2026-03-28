using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Represents a role within the platform's RBAC system.
/// Roles define a set of permissions and can be assigned to users within a tenant.
/// </summary>
public sealed class Role : AggregateRoot<Guid>
{
    private Role() { } // EF Core parameterless constructor

    public Role(string nameAr, string nameEn, string normalizedName, Guid tenantId, bool isSystemRole = false)
    {
        Id = Guid.NewGuid();
        NameAr = nameAr;
        NameEn = nameEn;
        NormalizedName = normalizedName.ToUpperInvariant();
        TenantId = tenantId;
        IsSystemRole = isSystemRole;
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

    /// <summary>Whether this is a system-defined role that cannot be deleted.</summary>
    public bool IsSystemRole { get; private set; }

    /// <summary>Whether this role is currently active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Concurrency stamp for optimistic concurrency control.</summary>
    public string ConcurrencyStamp { get; private set; } = Guid.NewGuid().ToString("N");

    /// <summary>Navigation property: users assigned to this role.</summary>
    public ICollection<UserRole> UserRoles { get; private set; } = [];

    /// <summary>Navigation property: permissions assigned to this role.</summary>
    public ICollection<RolePermission> RolePermissions { get; private set; } = [];

    // ----- Domain Methods -----

    public void UpdateName(string nameAr, string nameEn)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        NormalizedName = nameEn.ToUpperInvariant();
        LastModifiedAt = DateTime.UtcNow;
    }

    public void SetDescription(string? description)
    {
        Description = description;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
    }
}
