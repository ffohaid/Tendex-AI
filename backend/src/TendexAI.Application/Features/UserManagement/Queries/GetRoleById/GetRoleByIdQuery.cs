using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;

namespace TendexAI.Application.Features.UserManagement.Queries.GetRoleById;

/// <summary>
/// Query to retrieve a role by ID with its permissions.
/// </summary>
public sealed record GetRoleByIdQuery(
    Guid RoleId,
    Guid TenantId) : IQuery<RoleDetailDto>;
