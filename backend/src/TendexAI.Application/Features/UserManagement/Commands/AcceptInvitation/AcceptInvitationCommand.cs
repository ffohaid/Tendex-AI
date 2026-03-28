using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.UserManagement.Commands.AcceptInvitation;

/// <summary>
/// Command to accept a registration invitation and create the user account.
/// The invitee provides their password to complete registration.
/// </summary>
public sealed record AcceptInvitationCommand(
    string Token,
    string Password,
    string ConfirmPassword,
    string? PhoneNumber) : ICommand<Guid>;
