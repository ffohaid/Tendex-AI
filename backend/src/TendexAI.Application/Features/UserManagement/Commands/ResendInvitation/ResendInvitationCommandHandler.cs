using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.ResendInvitation;

/// <summary>
/// Handles the <see cref="ResendInvitationCommand"/>.
/// Resends the invitation email with a new token and extended expiration.
/// </summary>
public sealed class ResendInvitationCommandHandler : ICommandHandler<ResendInvitationCommand>
{
    private readonly IUserInvitationRepository _invitationRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResendInvitationCommandHandler> _logger;

    public ResendInvitationCommandHandler(
        IUserInvitationRepository invitationRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<ResendInvitationCommandHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ResendInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation is null)
            return Result.Failure("Invitation not found.");

        if (invitation.TenantId != request.TenantId)
            return Result.Failure("Invitation does not belong to the current tenant.");

        var resendResult = invitation.Resend();
        if (resendResult.IsFailure)
            return resendResult;

        _invitationRepository.Update(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send the invitation email with the new token
        var invitationLink = $"{request.BaseUrl.TrimEnd('/')}/auth/accept-invitation?token={invitation.Token}";
        var recipientName = $"{invitation.FirstNameAr} {invitation.LastNameAr}";

        var emailSent = await _emailService.SendInvitationEmailAsync(
            toEmail: invitation.Email,
            recipientName: recipientName,
            invitationLink: invitationLink,
            tenantName: request.TenantName,
            inviterName: request.InviterName,
            cancellationToken: cancellationToken);

        if (!emailSent)
        {
            _logger.LogWarning("Resend invitation email failed for invitation {InvitationId}", request.InvitationId);
        }

        _logger.LogInformation("Invitation {InvitationId} resent (count: {ResendCount})",
            request.InvitationId, invitation.ResendCount);

        return Result.Success();
    }
}
