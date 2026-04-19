using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.UserManagement.Commands.DeleteRole;

/// <summary>
/// Command to delete a custom role by its identifier.
/// Only non-system, non-protected roles with no assigned users can be deleted.
/// </summary>
/// <param name="RoleId">The unique identifier of the role to delete.</param>
/// <param name="TenantId">The tenant that owns the role.</param>
public sealed record DeleteRoleCommand(Guid RoleId, Guid TenantId) : ICommand;
