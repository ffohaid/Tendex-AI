using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;

namespace TendexAI.Application.Features.Committees.Commands.UpdateCommittee;

/// <summary>
/// Handles updating basic committee information.
/// </summary>
public sealed class UpdateCommitteeCommandHandler : ICommandHandler<UpdateCommitteeCommand>
{
    private readonly ICommitteeRepository _committeeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UpdateCommitteeCommandHandler> _logger;

    public UpdateCommitteeCommandHandler(
        ICommitteeRepository committeeRepository,
        ICurrentUserService currentUser,
        ILogger<UpdateCommitteeCommandHandler> logger)
    {
        _committeeRepository = committeeRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateCommitteeCommand request, CancellationToken cancellationToken)
    {
        var committee = await _committeeRepository.GetByIdWithMembersAsync(request.CommitteeId, cancellationToken);
        if (committee is null)
            return Result.Failure("Committee not found.");

        var userId = _currentUser.UserId?.ToString() ?? "system";
        var result = committee.UpdateInfo(request.NameAr, request.NameEn, request.Description, userId);
        if (result.IsFailure)
            return result;

        // Entity is already tracked by EF Core (loaded without AsNoTracking).
        await _committeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Committee {CommitteeId} updated by user {UserId}", committee.Id, userId);
        return Result.Success();
    }
}
