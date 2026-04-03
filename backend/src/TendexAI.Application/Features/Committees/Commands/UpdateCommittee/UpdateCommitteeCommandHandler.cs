using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.UpdateCommittee;

/// <summary>
/// Handles updating committee information including scope, phases, and competition links.
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

        // Update basic info
        var infoResult = committee.UpdateInfo(request.NameAr, request.NameEn, request.Description, userId);
        if (infoResult.IsFailure)
            return infoResult;

        // Update scope
        var scopeResult = committee.UpdateScope(
            request.ScopeType,
            request.ActiveFromPhase,
            request.ActiveToPhase,
            userId);
        if (scopeResult.IsFailure)
            return scopeResult;

        // Update competition links
        var requestedIds = request.CompetitionIds ?? [];
        var currentIds = committee.Competitions.Select(c => c.CompetitionId).ToHashSet();

        // Remove competitions no longer in the list
        var toRemove = currentIds.Except(requestedIds).ToList();
        foreach (var competitionId in toRemove)
        {
            var removeResult = committee.UnlinkCompetition(competitionId, userId);
            if (removeResult.IsFailure)
                return removeResult;
        }

        // Add new competitions
        var toAdd = requestedIds.Except(currentIds).ToList();
        foreach (var competitionId in toAdd)
        {
            var linkResult = committee.LinkCompetition(competitionId, userId);
            if (linkResult.IsFailure)
                return linkResult;
        }

        await _committeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Committee {CommitteeId} updated (Scope={ScopeType}) by user {UserId}",
            committee.Id, request.ScopeType, userId);

        return Result.Success();
    }
}
