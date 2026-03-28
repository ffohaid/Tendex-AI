using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.UserManagement.Commands.ToggleUserStatus;

/// <summary>
/// Command to activate or deactivate a user account.
/// </summary>
public sealed record ToggleUserStatusCommand(
    Guid UserId,
    bool Activate,
    Guid TenantId) : ICommand;
