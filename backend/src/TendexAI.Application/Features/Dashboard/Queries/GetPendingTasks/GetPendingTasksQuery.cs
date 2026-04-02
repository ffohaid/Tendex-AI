using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Dashboard.Dtos;

namespace TendexAI.Application.Features.Dashboard.Queries.GetPendingTasks;

/// <summary>
/// Query to retrieve pending tasks for the current user from the unified task center.
/// Tasks are derived from competitions requiring action, inquiry responses/approvals,
/// committee assignments, and pending approval workflow steps.
/// Supports filtering by type, priority, SLA status, source type, and search term.
/// </summary>
public sealed record GetPendingTasksQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? TypeFilter = null,
    string? PriorityFilter = null,
    string? SlaStatusFilter = null,
    string? SourceFilter = null,
    string? SearchTerm = null,
    string? SortBy = "priority") : IQuery<PendingTasksPagedResultDto>;
