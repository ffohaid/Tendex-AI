using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Dashboard.Dtos;

namespace TendexAI.Application.Features.Dashboard.Queries.GetRecentActivities;

/// <summary>
/// Query to retrieve recent activity log entries for the current tenant.
/// </summary>
public sealed record GetRecentActivitiesQuery(
    int PageNumber = 1,
    int PageSize = 10) : IQuery<RecentActivitiesPagedResultDto>;
