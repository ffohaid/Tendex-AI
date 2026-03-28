using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;

namespace TendexAI.Application.Features.Committees.Commands.RemoveCommitteeMember;

/// <summary>
/// Handles removing (deactivating) a member from a committee.
/// </summary>
public sealed class RemoveCommitteeMemberCommandHandler : ICommandHandler<RemoveCommitteeMemberCommand>
{
    private readonly ICommitteeRepository _committeeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<RemoveCommitteeMemberCommandHandler> _logger;

    public RemoveCommitteeMemberCommandHandler(
        ICommitteeRepository committeeRepository,
        ICurrentUserService currentUser,
        ILogger<RemoveCommitteeMemberCommandHandler> logger)
    {
        _committeeRepository = committeeRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveCommitteeMemberCommand request, CancellationToken cancellationToken)
    {
        var committee = await _committeeRepository.GetByIdWithMembersAsync(
            request.CommitteeId, cancellationToken);
        if (committee is null)
            return Result.Failure("Committee not found.");

        var removedBy = _currentUser.UserId?.ToString() ?? "system";

        var result = committee.RemoveMember(request.UserId, removedBy, request.Reason);
        if (result.IsFailure)
            return result;

        _committeeRepository.Update(committee);
        await _committeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} removed from committee {CommitteeId} by {RemovedBy}. Reason: {Reason}",
            request.UserId, committee.Id, removedBy, request.Reason);

        return Result.Success();
    }
}
