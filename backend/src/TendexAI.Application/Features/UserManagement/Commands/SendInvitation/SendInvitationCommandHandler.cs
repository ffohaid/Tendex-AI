using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.SendInvitation;

/// <summary>
/// Handles the <see cref="SendInvitationCommand"/>.
/// Creates a new invitation record and sends the invitation email.
/// </summary>
public sealed class SendInvitationCommandHandler : ICommandHandler<SendInvitationCommand, Guid>
{
    private readonly IUserInvitationRepository _invitationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendInvitationCommandHandler> _logger;

    public SendInvitationCommandHandler(
        IUserInvitationRepository invitationRepository,
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<SendInvitationCommandHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(SendInvitationCommand request, CancellationToken cancellationToken)
    {
        // Check if user already exists with this email
        var existingUser = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (existingUser)
        {
            _logger.LogWarning("Invitation failed: user with email {Email} already exists", request.Email);
            return Result.Failure<Guid>("A user with this email address already exists.");
        }

        // Check if there is already a pending invitation for this email in this tenant
        var hasPending = await _invitationRepository.HasPendingInvitationAsync(
            request.Email, request.TenantId, cancellationToken);
        if (hasPending)
        {
            _logger.LogWarning("Invitation failed: pending invitation already exists for {Email}", request.Email);
            return Result.Failure<Guid>("A pending invitation already exists for this email address.");
        }

        // Create the invitation
        var invitation = new UserInvitation(
            email: request.Email,
            firstNameAr: request.FirstNameAr,
            lastNameAr: request.LastNameAr,
            tenantId: request.TenantId,
            invitedByUserId: request.InvitedByUserId,
            roleId: request.RoleId,
            firstNameEn: request.FirstNameEn,
            lastNameEn: request.LastNameEn);

        await _invitationRepository.AddAsync(invitation, cancellationToken);
        // CRITICAL FIX: Use repository's SaveChangesAsync which operates on TenantDbContext
        // instead of IUnitOfWork which points to MasterPlatformDbContext
        await _invitationRepository.SaveChangesAsync(cancellationToken);

        // Send the invitation email
        var invitationLink = $"{request.BaseUrl.TrimEnd('/')}/auth/accept-invitation?token={invitation.Token}";
        var recipientName = $"{request.FirstNameAr} {request.LastNameAr}";

        var emailSent = await _emailService.SendInvitationEmailAsync(
            toEmail: request.Email,
            recipientName: recipientName,
            invitationLink: invitationLink,
            tenantName: request.TenantName,
            inviterName: request.InviterName,
            cancellationToken: cancellationToken);

        if (!emailSent)
        {
            _logger.LogWarning("Invitation email failed to send for {Email}, but invitation was created", request.Email);
        }

        _logger.LogInformation("Invitation {InvitationId} sent to {Email} for tenant {TenantId}",
            invitation.Id, request.Email, request.TenantId);

        return Result.Success(invitation.Id);
    }
}
