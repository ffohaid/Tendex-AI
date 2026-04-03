using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.UserManagement.Commands.ToggleRoleStatus;

/// <summary>
/// Command to activate or deactivate a role.
/// System roles cannot be deactivated.
/// </summary>
public sealed record ToggleRoleStatusCommand(
    Guid RoleId,
    bool Activate,
    Guid TenantId) : ICommand;
