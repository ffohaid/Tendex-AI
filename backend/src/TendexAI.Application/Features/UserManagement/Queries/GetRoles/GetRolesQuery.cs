using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;

namespace TendexAI.Application.Features.UserManagement.Queries.GetRoles;

/// <summary>
/// Query to retrieve all roles for a tenant.
/// </summary>
public sealed record GetRolesQuery(Guid TenantId) : IQuery<IReadOnlyList<RoleDto>>;
