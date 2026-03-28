using System.Diagnostics.CodeAnalysis;
using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Represents a granular permission in the RBAC system.
/// Permissions are grouped by module and can be assigned to roles.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Permission is the correct domain term.")]
public sealed class Permission : BaseEntity<Guid>
{
    private Permission() { } // EF Core parameterless constructor

    public Permission(string code, string nameAr, string nameEn, string module)
    {
        Id = Guid.NewGuid();
        Code = code;
        NameAr = nameAr;
        NameEn = nameEn;
        Module = module;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Unique permission code (e.g., "rfp.create", "rfp.approve").</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Arabic display name of the permission.</summary>
    public string NameAr { get; private set; } = null!;

    /// <summary>English display name of the permission.</summary>
    public string NameEn { get; private set; } = null!;

    /// <summary>Module this permission belongs to (e.g., "RFP", "Users", "Settings").</summary>
    public string Module { get; private set; } = null!;

    /// <summary>Optional description of the permission.</summary>
    public string? Description { get; private set; }

    /// <summary>Navigation property: roles that have this permission.</summary>
    public ICollection<RolePermission> RolePermissions { get; private set; } = [];
}
