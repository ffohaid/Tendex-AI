using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;

namespace TendexAI.Application.Features.UserManagement.Queries.GetUsers;

/// <summary>
/// Query to retrieve a paginated and filterable list of users for a tenant.
/// Supports search by name/email, filter by role and status.
/// </summary>
public sealed record GetUsersQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    Guid? RoleId = null,
    bool? IsActive = null) : IQuery<PaginatedResult<UserDto>>;
