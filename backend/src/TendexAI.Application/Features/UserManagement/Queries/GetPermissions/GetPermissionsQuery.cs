using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;

namespace TendexAI.Application.Features.UserManagement.Queries.GetPermissions;

/// <summary>
/// Query to retrieve all available permissions grouped by module.
/// </summary>
public sealed record GetPermissionsQuery() : IQuery<IReadOnlyList<PermissionGroupDto>>;
