using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.UserManagement.Commands.SendInvitation;

/// <summary>
/// Command to send a registration invitation to a new user.
/// Only administrators (Owner, Admin, SectorRep) can send invitations.
/// Self-registration is not allowed.
/// </summary>
public sealed record SendInvitationCommand(
    string Email,
    string FirstNameAr,
    string LastNameAr,
    string? FirstNameEn,
    string? LastNameEn,
    Guid? RoleId,
    Guid TenantId,
    Guid InvitedByUserId,
    string InviterName,
    string TenantName,
    string BaseUrl) : ICommand<Guid>;
