using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.UserManagement.Commands.AssignRole;

/// <summary>
/// Command to assign a role to a user.
/// </summary>
public sealed record AssignRoleCommand(
    Guid UserId,
    Guid RoleId,
    Guid TenantId,
    string AssignedBy) : ICommand;
