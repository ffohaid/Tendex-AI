using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Features.Impersonation.Queries;

/// <summary>
/// Handles searching users across all tenants for impersonation purposes.
/// Delegates to ICrossTenantUserSearchService (implemented in Infrastructure)
/// to respect Clean Architecture boundaries.
/// </summary>
public sealed class SearchUsersQueryHandler
    : IQueryHandler<SearchUsersQuery, PaginatedResponse<UserSearchResultDto>>
{
    private readonly ICrossTenantUserSearchService _userSearchService;

    public SearchUsersQueryHandler(ICrossTenantUserSearchService userSearchService)
    {
        _userSearchService = userSearchService;
    }

    public async Task<Result<PaginatedResponse<UserSearchResultDto>>> Handle(
        SearchUsersQuery request,
        CancellationToken cancellationToken)
    {
        var searchResult = await _userSearchService.SearchUsersAsync(
            request.SearchTerm,
            request.TenantId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtoItems = searchResult.Items
            .Select(u => new UserSearchResultDto(
                u.UserId,
                u.Email,
                u.FirstName,
                u.LastName,
                u.TenantId,
                u.TenantName,
                u.IsActive,
                u.LastLoginAt))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PaginatedResponse<UserSearchResultDto>(
            Items: dtoItems,
            TotalCount: searchResult.TotalCount,
            Page: searchResult.Page,
            PageSize: searchResult.PageSize));
    }
}
