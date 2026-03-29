using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Dashboard.Dtos;

namespace TendexAI.Application.Features.Dashboard.Queries.GetDashboardStats;

/// <summary>
/// Query to retrieve aggregated dashboard statistics (KPI cards) for the current tenant.
/// </summary>
public sealed record GetDashboardStatsQuery() : IQuery<DashboardStatsDto>;
