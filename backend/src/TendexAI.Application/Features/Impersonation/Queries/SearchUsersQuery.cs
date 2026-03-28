using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;

namespace TendexAI.Application.Features.Impersonation.Queries;

/// <summary>
/// Query to search users across all tenants for impersonation purposes.
/// Supports searching by email, name, or tenant.
/// </summary>
public sealed record SearchUsersQuery(
    string? SearchTerm = null,
    Guid? TenantId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResponse<UserSearchResultDto>>;
