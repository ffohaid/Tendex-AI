using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Dashboard.Dtos;

namespace TendexAI.Application.Features.Dashboard.Queries.GetPerformanceMetrics;

/// <summary>
/// Query to retrieve performance metrics for dashboard charts.
/// </summary>
public sealed record GetPerformanceMetricsQuery() : IQuery<PerformanceMetricsDto>;
