using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;

namespace TendexAI.Application.Features.UserManagement.Queries.GetUsers;

/// <summary>
/// Query to retrieve a paginated list of users for a tenant.
/// </summary>
public sealed record GetUsersQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResult<UserDto>>;
