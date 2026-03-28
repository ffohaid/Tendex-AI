using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Represents an immutable audit trail entry.
/// This entity follows an Append-Only pattern: once created, it MUST NOT be updated or deleted.
/// All properties are set exclusively through the constructor and have no public setters.
/// Aligned with PRD v6 Section 20 requirements.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>
    /// Private parameterless constructor for EF Core materialization only.
    /// </summary>
    private AuditLogEntry() { }

    /// <summary>
    /// Creates a new immutable audit log entry with all required fields.
    /// </summary>
    public AuditLogEntry(
        Guid userId,
        string userName,
        string? ipAddress,
        AuditActionType actionType,
        string entityType,
        string entityId,
        string? oldValues,
        string? newValues,
        string? reason,
        string? sessionId,
        Guid? tenantId)
    {
        Id = Guid.NewGuid();
        TimestampUtc = DateTime.UtcNow;
        UserId = userId;
        UserName = userName;
        IpAddress = ipAddress;
        ActionType = actionType;
        EntityType = entityType;
        EntityId = entityId;
        OldValues = oldValues;
        NewValues = newValues;
        Reason = reason;
        SessionId = sessionId;
        TenantId = tenantId;
    }

    /// <summary>Auto-generated unique identifier for the audit record.</summary>
    public Guid Id { get; private set; }

    /// <summary>UTC timestamp of the action with millisecond precision.</summary>
    public DateTime TimestampUtc { get; private set; }

    /// <summary>Identifier of the user who performed the action.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Full name of the user who performed the action.</summary>
    public string UserName { get; private set; } = null!;

    /// <summary>IP address of the user at the time of the action.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Type of the action performed (Create, Update, Delete, Approve, etc.).</summary>
    public AuditActionType ActionType { get; private set; }

    /// <summary>Type of the affected entity (e.g., "Rfp", "Committee", "User").</summary>
    public string EntityType { get; private set; } = null!;

    /// <summary>Identifier of the affected entity.</summary>
    public string EntityId { get; private set; } = null!;

    /// <summary>JSON representation of the entity values before the modification.</summary>
    public string? OldValues { get; private set; }

    /// <summary>JSON representation of the entity values after the modification.</summary>
    public string? NewValues { get; private set; }

    /// <summary>Optional reason or justification for the action.</summary>
    public string? Reason { get; private set; }

    /// <summary>Active session identifier at the time of the action.</summary>
    public string? SessionId { get; private set; }

    /// <summary>Tenant (government entity) identifier for multi-tenant isolation.</summary>
    public Guid? TenantId { get; private set; }
}
