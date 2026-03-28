using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.Tenants.Queries.GetTenantsList;

/// <summary>
/// Handles retrieving a paginated list of tenants.
/// </summary>
public sealed class GetTenantsListQueryHandler
    : IQueryHandler<GetTenantsListQuery, PagedResultDto<TenantListItemDto>>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantsListQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<PagedResultDto<TenantListItemDto>>> Handle(
        GetTenantsListQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize > 100 ? 100 : request.PageSize;

        var (items, totalCount) = await _tenantRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            request.SearchTerm,
            request.StatusFilter,
            cancellationToken);

        var dtos = items.Select(TenantMapper.MapToListItemDto).ToList();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var result = new PagedResultDto<TenantListItemDto>(
            Items: dtos,
            TotalCount: totalCount,
            PageNumber: pageNumber,
            PageSize: pageSize,
            TotalPages: totalPages);

        return Result.Success(result);
    }
}
