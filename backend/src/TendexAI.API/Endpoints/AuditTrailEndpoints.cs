using System.Globalization;
using System.Text;
using MediatR;
using TendexAI.Application.AuditTrail.Queries;
using TendexAI.Domain.Enums;

namespace TendexAI.API.Endpoints;

/// <summary>
/// Minimal API endpoints for querying the immutable audit trail.
/// These endpoints are read-only (GET) — no POST/PUT/DELETE operations are exposed
/// because audit entries are created automatically by the system interceptors.
/// </summary>
public static class AuditTrailEndpoints
{
    /// <summary>
    /// Maps all audit trail related endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapAuditTrailEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/audit-logs")
            .WithTags("Audit Trail");

        // GET /api/v1/audit-logs
        group.MapGet("/", GetAuditLogs)
            .WithName("GetAuditLogs")
            .WithSummary("Retrieves paginated audit log entries with optional filters.")
            .Produces<GetAuditLogsResult>(StatusCodes.Status200OK);

        // GET /api/v1/audit-logs/export
        group.MapGet("/export", ExportAuditLogs)
            .WithName("ExportAuditLogs")
            .WithSummary("Exports audit log entries as CSV file.")
            .Produces(StatusCodes.Status200OK);

        // GET /api/v1/audit-logs/{id}
        group.MapGet("/{id:guid}", GetAuditLogById)
            .WithName("GetAuditLogById")
            .WithSummary("Retrieves a single audit log entry by its unique identifier.")
            .Produces<AuditLogDetailResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/v1/audit-logs/entity/{entityType}/{entityId}
        group.MapGet("/entity/{entityType}/{entityId}", GetAuditLogsByEntity)
            .WithName("GetAuditLogsByEntity")
            .WithSummary("Retrieves audit log entries for a specific entity.")
            .Produces<GetAuditLogsResult>(StatusCodes.Status200OK);

        // GET /api/v1/audit-logs/action-types
        group.MapGet("/action-types", GetActionTypes)
            .WithName("GetAuditActionTypes")
            .WithSummary("Returns all available audit action types.")
            .Produces<IEnumerable<ActionTypeResponse>>(StatusCodes.Status200OK);

        return app;
    }

    /// <summary>
    /// Retrieves paginated audit log entries with optional filters.
    /// </summary>
    private static async Task<IResult> GetAuditLogs(
        ISender mediator,
        Guid? tenantId = null,
        Guid? userId = null,
        AuditActionType? actionType = null,
        string? entityType = null,
        string? entityId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = new GetAuditLogsQuery(
            TenantId: tenantId,
            UserId: userId,
            ActionType: actionType,
            EntityType: entityType,
            EntityId: entityId,
            FromUtc: fromUtc,
            ToUtc: toUtc,
            Page: page,
            PageSize: pageSize);

        var result = await mediator.Send(query);
        return Results.Ok(result);
    }

    /// <summary>
    /// Exports audit log entries as a CSV file.
    /// </summary>
    private static async Task<IResult> ExportAuditLogs(
        ISender mediator,
        Guid? tenantId = null,
        Guid? userId = null,
        AuditActionType? actionType = null,
        string? entityType = null,
        string? entityId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null)
    {
        // Fetch up to 10000 records for export
        var query = new GetAuditLogsQuery(
            TenantId: tenantId,
            UserId: userId,
            ActionType: actionType,
            EntityType: entityType,
            EntityId: entityId,
            FromUtc: fromUtc,
            ToUtc: toUtc,
            Page: 1,
            PageSize: 10000);

        var result = await mediator.Send(query);

        var sb = new StringBuilder();
        // CSV Header with BOM for Arabic support
        sb.AppendLine("Id,Timestamp,UserName,UserId,ActionType,EntityType,EntityId,IpAddress,Reason,TenantId,SessionId");

        foreach (var entry in result.Items)
        {
            var reason = (entry.Reason ?? "").Replace("\"", "\"\"");
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"\"{entry.Id}\"," +
                $"\"{entry.TimestampUtc:yyyy-MM-dd HH:mm:ss}\"," +
                $"\"{entry.UserName}\"," +
                $"\"{entry.UserId}\"," +
                $"\"{entry.ActionType}\"," +
                $"\"{entry.EntityType}\"," +
                $"\"{entry.EntityId}\"," +
                $"\"{entry.IpAddress ?? ""}\"," +
                $"\"{reason}\"," +
                $"\"{entry.TenantId?.ToString() ?? ""}\"," +
                $"\"{entry.SessionId ?? ""}\"");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return Results.File(bytes, "text/csv; charset=utf-8", $"audit-log-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    /// <summary>
    /// Retrieves a single audit log entry by its unique identifier.
    /// </summary>
    private static async Task<IResult> GetAuditLogById(
        ISender mediator,
        Guid id)
    {
        var query = new GetAuditLogByIdQuery(id);
        var entry = await mediator.Send(query);

        if (entry is null)
            return Results.NotFound(new { Message = "Audit log entry not found.", Id = id });

        return Results.Ok(new AuditLogDetailResponse(
            Id: entry.Id,
            TimestampUtc: entry.TimestampUtc,
            UserId: entry.UserId,
            UserName: entry.UserName,
            IpAddress: entry.IpAddress,
            ActionType: entry.ActionType.ToString(),
            EntityType: entry.EntityType,
            EntityId: entry.EntityId,
            OldValues: entry.OldValues,
            NewValues: entry.NewValues,
            Reason: entry.Reason,
            SessionId: entry.SessionId,
            TenantId: entry.TenantId));
    }

    /// <summary>
    /// Retrieves audit log entries for a specific entity.
    /// </summary>
    private static async Task<IResult> GetAuditLogsByEntity(
        ISender mediator,
        string entityType,
        string entityId,
        Guid? tenantId = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = new GetAuditLogsQuery(
            TenantId: tenantId,
            EntityType: entityType,
            EntityId: entityId,
            Page: page,
            PageSize: pageSize);

        var result = await mediator.Send(query);
        return Results.Ok(result);
    }

    /// <summary>
    /// Returns all available audit action types for filtering.
    /// </summary>
    private static IResult GetActionTypes()
    {
        var actionTypes = Enum.GetValues<AuditActionType>()
            .Select(a => new ActionTypeResponse(
                Value: (int)a,
                Name: a.ToString()))
            .ToList();

        return Results.Ok(actionTypes);
    }
}

// ----- Response DTOs -----

/// <summary>
/// Detailed response for a single audit log entry.
/// </summary>
public sealed record AuditLogDetailResponse(
    Guid Id,
    DateTime TimestampUtc,
    Guid UserId,
    string UserName,
    string? IpAddress,
    string ActionType,
    string EntityType,
    string EntityId,
    string? OldValues,
    string? NewValues,
    string? Reason,
    string? SessionId,
    Guid? TenantId);

/// <summary>
/// Response for available audit action types.
/// </summary>
public sealed record ActionTypeResponse(
    int Value,
    string Name);
