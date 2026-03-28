using MediatR;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.AuditTrail.Queries;

/// <summary>
/// Query to retrieve paginated audit log entries with optional filters.
/// </summary>
public sealed record GetAuditLogsQuery(
    Guid? TenantId = null,
    Guid? UserId = null,
    AuditActionType? ActionType = null,
    string? EntityType = null,
    string? EntityId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 50) : IRequest<GetAuditLogsResult>;

/// <summary>
/// Result containing paginated audit log entries and metadata.
/// </summary>
public sealed record GetAuditLogsResult(
    IReadOnlyList<AuditLogEntry> Items,
    long TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
