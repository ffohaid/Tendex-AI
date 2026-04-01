using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Features.Impersonation.Dtos;

namespace TendexAI.Infrastructure.Services;

/// <summary>
/// Stub implementation of ICrossTenantUserSearchService.
/// Returns empty results until full cross-tenant search is implemented.
/// </summary>
public sealed class CrossTenantUserSearchService : ICrossTenantUserSearchService
{
    public Task<PaginatedResponse<CrossTenantUserResult>> SearchUsersAsync(
        string? searchTerm,
        Guid? tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new PaginatedResponse<CrossTenantUserResult>(
            Items: Array.Empty<CrossTenantUserResult>(),
            TotalCount: 0,
            Page: page,
            PageSize: pageSize));
    }
}
