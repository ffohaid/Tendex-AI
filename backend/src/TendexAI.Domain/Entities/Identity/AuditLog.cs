using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Represents an immutable audit log entry (Append-Only).
/// Records all authentication and authorization actions for compliance.
/// No UPDATE or DELETE operations are permitted on this entity.
/// </summary>
public sealed class AuditLog : BaseEntity<Guid>
{
    private AuditLog() { } // EF Core parameterless constructor

    public AuditLog(
        Guid? userId,
        string action,
        string entityType,
        string? entityId,
        string? oldValues,
        string? newValues,
        string ipAddress,
        string? userAgent,
        Guid? tenantId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        OldValues = oldValues;
        NewValues = newValues;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        TenantId = tenantId;
        Timestamp = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>The user who performed the action (null for anonymous actions).</summary>
    public Guid? UserId { get; private set; }

    /// <summary>The action performed (e.g., "Login", "FailedLogin", "MfaEnabled").</summary>
    public string Action { get; private set; } = null!;

    /// <summary>The type of entity affected.</summary>
    public string EntityType { get; private set; } = null!;

    /// <summary>The identifier of the affected entity.</summary>
    public string? EntityId { get; private set; }

    /// <summary>JSON representation of old values (before change).</summary>
    public string? OldValues { get; private set; }

    /// <summary>JSON representation of new values (after change).</summary>
    public string? NewValues { get; private set; }

    /// <summary>IP address of the request origin.</summary>
    public string IpAddress { get; private set; } = null!;

    /// <summary>User agent string from the request.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>The tenant context of this audit entry.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Precise timestamp of the action (UTC).</summary>
    public DateTime Timestamp { get; private set; }
}
