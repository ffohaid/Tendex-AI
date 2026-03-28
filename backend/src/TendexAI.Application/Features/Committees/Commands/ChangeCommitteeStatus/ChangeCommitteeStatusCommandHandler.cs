using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.ChangeCommitteeStatus;

/// <summary>
/// Handles changing the lifecycle status of a committee.
/// </summary>
public sealed class ChangeCommitteeStatusCommandHandler : ICommandHandler<ChangeCommitteeStatusCommand>
{
    private readonly ICommitteeRepository _committeeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ChangeCommitteeStatusCommandHandler> _logger;

    public ChangeCommitteeStatusCommandHandler(
        ICommitteeRepository committeeRepository,
        ICurrentUserService currentUser,
        ILogger<ChangeCommitteeStatusCommandHandler> logger)
    {
        _committeeRepository = committeeRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(ChangeCommitteeStatusCommand request, CancellationToken cancellationToken)
    {
        var committee = await _committeeRepository.GetByIdWithMembersAsync(
            request.CommitteeId, cancellationToken);
        if (committee is null)
            return Result.Failure("Committee not found.");

        var userId = _currentUser.UserId?.ToString() ?? "system";

        var result = request.NewStatus switch
        {
            CommitteeStatus.Suspended => committee.Suspend(userId, request.Reason ?? string.Empty),
            CommitteeStatus.Active => committee.Reactivate(userId),
            CommitteeStatus.Dissolved => committee.Dissolve(userId, request.Reason ?? string.Empty),
            _ => Result.Failure($"Invalid status transition to {request.NewStatus}.")
        };

        if (result.IsFailure)
            return result;

        _committeeRepository.Update(committee);
        await _committeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Committee {CommitteeId} status changed to {NewStatus} by user {UserId}",
            committee.Id, request.NewStatus, userId);

        return Result.Success();
    }
}
