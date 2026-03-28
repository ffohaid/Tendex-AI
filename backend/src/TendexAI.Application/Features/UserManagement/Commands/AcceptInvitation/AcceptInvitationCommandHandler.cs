using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.AcceptInvitation;

/// <summary>
/// Handles the <see cref="AcceptInvitationCommand"/>.
/// Validates the invitation token, creates the user account, assigns the role, and marks the invitation as accepted.
/// </summary>
public sealed class AcceptInvitationCommandHandler : ICommandHandler<AcceptInvitationCommand, Guid>
{
    private readonly IUserInvitationRepository _invitationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptInvitationCommandHandler> _logger;

    public AcceptInvitationCommandHandler(
        IUserInvitationRepository invitationRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ILogger<AcceptInvitationCommandHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        // Find the invitation by token
        var invitation = await _invitationRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (invitation is null)
        {
            _logger.LogWarning("Accept invitation failed: invalid token");
            return Result.Failure<Guid>("Invalid or expired invitation token.");
        }

        // Validate the invitation is still valid
        if (!invitation.IsValid())
        {
            _logger.LogWarning("Accept invitation failed: invitation {InvitationId} is no longer valid (Status: {Status})",
                invitation.Id, invitation.Status);
            return Result.Failure<Guid>("This invitation is no longer valid. It may have expired or been revoked.");
        }

        // Check if user already exists
        var existingUser = await _userRepository.ExistsByEmailAsync(invitation.Email, cancellationToken);
        if (existingUser)
        {
            _logger.LogWarning("Accept invitation failed: user with email {Email} already exists", invitation.Email);
            return Result.Failure<Guid>("A user with this email address already exists.");
        }

        // Create the user account
        var user = new ApplicationUser(
            email: invitation.Email,
            firstName: invitation.FirstNameAr,
            lastName: invitation.LastNameAr,
            phoneNumber: request.PhoneNumber,
            tenantId: invitation.TenantId);

        // Set the password
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        user.SetPasswordHash(passwordHash);
        user.ConfirmEmail(); // Email is confirmed via invitation

        await _userRepository.AddAsync(user, cancellationToken);

        // Assign the role if specified in the invitation
        if (invitation.RoleId.HasValue)
        {
            var userRole = new UserRole(
                userId: user.Id,
                roleId: invitation.RoleId.Value,
                assignedBy: invitation.InvitedByUserId.ToString());

            await _userRepository.AddUserRoleAsync(userRole, cancellationToken);
        }

        // Mark the invitation as accepted
        var acceptResult = invitation.Accept(user.Id);
        if (acceptResult.IsFailure)
        {
            _logger.LogWarning("Accept invitation failed: {Error}", acceptResult.Error);
            return Result.Failure<Guid>(acceptResult.Error!);
        }

        _invitationRepository.Update(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} created from invitation {InvitationId} for tenant {TenantId}",
            user.Id, invitation.Id, invitation.TenantId);

        return Result.Success(user.Id);
    }
}
