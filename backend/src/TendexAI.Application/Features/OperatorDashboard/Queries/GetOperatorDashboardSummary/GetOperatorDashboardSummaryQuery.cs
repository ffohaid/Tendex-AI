using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.OperatorDashboard.Dtos;

namespace TendexAI.Application.Features.OperatorDashboard.Queries.GetOperatorDashboardSummary;

/// <summary>
/// Query to retrieve the aggregated operator dashboard summary.
/// Returns high-level KPIs, status distributions, and trend data across all tenants.
/// </summary>
public sealed record GetOperatorDashboardSummaryQuery() : IQuery<OperatorDashboardSummaryDto>;
