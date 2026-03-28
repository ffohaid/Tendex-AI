using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Represents the many-to-many relationship between users and roles.
/// Includes assignment metadata for audit purposes.
/// </summary>
public sealed class UserRole : BaseEntity<Guid>
{
    private UserRole() { } // EF Core parameterless constructor

    public UserRole(Guid userId, Guid roleId, string? assignedBy)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RoleId = roleId;
        AssignedBy = assignedBy;
        AssignedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>The user who is assigned the role.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Navigation property to the user.</summary>
    public ApplicationUser User { get; private set; } = null!;

    /// <summary>The role assigned to the user.</summary>
    public Guid RoleId { get; private set; }

    /// <summary>Navigation property to the role.</summary>
    public Role Role { get; private set; } = null!;

    /// <summary>Timestamp when the role was assigned.</summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>Identifier of the admin who assigned this role.</summary>
    public string? AssignedBy { get; private set; }
}
