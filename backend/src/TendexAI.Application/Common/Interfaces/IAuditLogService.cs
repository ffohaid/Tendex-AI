using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Service interface for recording and querying immutable audit trail entries.
/// Implementation resides in the Infrastructure layer.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Records a new audit log entry. This is an append-only operation.
    /// </summary>
    Task LogAsync(
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
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit log entries filtered by the specified criteria.
    /// </summary>
    Task<IReadOnlyList<AuditLogEntry>> GetLogsAsync(
        Guid? tenantId = null,
        Guid? userId = null,
        AuditActionType? actionType = null,
        string? entityType = null,
        string? entityId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the total count of audit log entries matching the specified criteria.
    /// </summary>
    Task<long> GetLogsCountAsync(
        Guid? tenantId = null,
        Guid? userId = null,
        AuditActionType? actionType = null,
        string? entityType = null,
        string? entityId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single audit log entry by its unique identifier.
    /// </summary>
    Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
