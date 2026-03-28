using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.UserManagement.Commands.RemoveRole;

/// <summary>
/// Command to remove a role from a user.
/// </summary>
public sealed record RemoveRoleCommand(
    Guid UserId,
    Guid RoleId,
    Guid TenantId) : ICommand;
