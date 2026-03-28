using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.OperatorDashboard.Dtos;
using TendexAI.Application.Features.Tenants.Dtos;

namespace TendexAI.Application.Features.OperatorDashboard.Queries.GetTenantUsageStatistics;

/// <summary>
/// Query to retrieve per-tenant usage statistics for the cross-tenant analytics view.
/// Supports pagination and optional search filtering.
/// </summary>
public sealed record GetTenantUsageStatisticsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null) : IQuery<PagedResultDto<TenantUsageStatisticsDto>>;
