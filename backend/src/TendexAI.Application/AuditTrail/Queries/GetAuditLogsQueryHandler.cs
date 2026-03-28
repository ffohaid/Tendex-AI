using MediatR;
using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Application.AuditTrail.Queries;

/// <summary>
/// Handles the <see cref="GetAuditLogsQuery"/> by delegating to <see cref="IAuditLogService"/>.
/// </summary>
public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, GetAuditLogsResult>
{
    private readonly IAuditLogService _auditLogService;

    public GetAuditLogsQueryHandler(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    public async Task<GetAuditLogsResult> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        // Clamp page and pageSize to valid ranges
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var items = await _auditLogService.GetLogsAsync(
            tenantId: request.TenantId,
            userId: request.UserId,
            actionType: request.ActionType,
            entityType: request.EntityType,
            entityId: request.EntityId,
            fromUtc: request.FromUtc,
            toUtc: request.ToUtc,
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        var totalCount = await _auditLogService.GetLogsCountAsync(
            tenantId: request.TenantId,
            userId: request.UserId,
            actionType: request.ActionType,
            entityType: request.EntityType,
            entityId: request.EntityId,
            fromUtc: request.FromUtc,
            toUtc: request.ToUtc,
            cancellationToken: cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new GetAuditLogsResult(
            Items: items,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            TotalPages: totalPages);
    }
}
