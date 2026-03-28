using System.Diagnostics.CodeAnalysis;
using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Represents the many-to-many relationship between roles and permissions.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "RolePermission is the correct domain term.")]
public sealed class RolePermission : BaseEntity<Guid>
{
    private RolePermission() { } // EF Core parameterless constructor

    public RolePermission(Guid roleId, Guid permissionId)
    {
        Id = Guid.NewGuid();
        RoleId = roleId;
        PermissionId = permissionId;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>The role that has this permission.</summary>
    public Guid RoleId { get; private set; }

    /// <summary>Navigation property to the role.</summary>
    public Role Role { get; private set; } = null!;

    /// <summary>The permission assigned to the role.</summary>
    public Guid PermissionId { get; private set; }

    /// <summary>Navigation property to the permission.</summary>
    public Permission Permission { get; private set; } = null!;
}
