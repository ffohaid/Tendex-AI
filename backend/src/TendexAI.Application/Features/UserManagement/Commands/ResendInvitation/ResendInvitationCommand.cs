using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.UserManagement.Commands.ResendInvitation;

/// <summary>
/// Command to resend a pending invitation email.
/// </summary>
public sealed record ResendInvitationCommand(
    Guid InvitationId,
    Guid TenantId,
    string InviterName,
    string TenantName,
    string BaseUrl) : ICommand;
