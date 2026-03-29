using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Dashboard.Dtos;

namespace TendexAI.Application.Features.Dashboard.Queries.GetPendingTasks;

/// <summary>
/// Query to retrieve pending tasks for the current user from the unified task center.
/// Tasks are derived from competitions requiring action, committee assignments,
/// and pending approval workflow steps.
/// </summary>
public sealed record GetPendingTasksQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? TypeFilter = null,
    string? PriorityFilter = null) : IQuery<PendingTasksPagedResultDto>;
