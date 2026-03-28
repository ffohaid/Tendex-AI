using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.OperatorDashboard.Dtos;

namespace TendexAI.Application.Features.OperatorDashboard.Queries.GetResourceConsumptionTrends;

/// <summary>
/// Query to retrieve resource consumption trends for the operator dashboard.
/// Returns daily audit log entries, daily new tenants, feature adoption rates,
/// and AI provider usage distribution.
/// </summary>
public sealed record GetResourceConsumptionTrendsQuery() : IQuery<ResourceConsumptionTrendsDto>;
