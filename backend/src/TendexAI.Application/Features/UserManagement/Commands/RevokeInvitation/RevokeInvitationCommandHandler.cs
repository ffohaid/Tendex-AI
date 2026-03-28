using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.RevokeInvitation;

/// <summary>
/// Handles the <see cref="RevokeInvitationCommand"/>.
/// Revokes a pending invitation.
/// </summary>
public sealed class RevokeInvitationCommandHandler : ICommandHandler<RevokeInvitationCommand>
{
    private readonly IUserInvitationRepository _invitationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevokeInvitationCommandHandler> _logger;

    public RevokeInvitationCommandHandler(
        IUserInvitationRepository invitationRepository,
        IUnitOfWork unitOfWork,
        ILogger<RevokeInvitationCommandHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RevokeInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken);
        if (invitation is null)
            return Result.Failure("Invitation not found.");

        if (invitation.TenantId != request.TenantId)
            return Result.Failure("Invitation does not belong to the current tenant.");

        var result = invitation.Revoke();
        if (result.IsFailure)
            return result;

        _invitationRepository.Update(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invitation {InvitationId} revoked", request.InvitationId);
        return Result.Success();
    }
}
