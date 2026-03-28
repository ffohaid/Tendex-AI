using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.UserManagement.Commands.RevokeInvitation;

/// <summary>
/// Command to revoke a pending invitation.
/// </summary>
public sealed record RevokeInvitationCommand(
    Guid InvitationId,
    Guid TenantId) : ICommand;
