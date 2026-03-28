using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Tenants.Queries.GetTenantsList;

/// <summary>
/// Query to retrieve a paginated list of tenants with optional filtering.
/// </summary>
public sealed record GetTenantsListQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    TenantStatus? StatusFilter = null) : IQuery<PagedResultDto<TenantListItemDto>>;
