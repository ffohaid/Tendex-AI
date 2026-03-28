using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.AddCommitteeMember;

/// <summary>
/// Handles adding a member to a committee.
/// Enforces conflict of interest rules (PRD Section 4.2, Section 23 Rules #3, #7).
/// </summary>
public sealed class AddCommitteeMemberCommandHandler : ICommandHandler<AddCommitteeMemberCommand>
{
    private readonly ICommitteeRepository _committeeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AddCommitteeMemberCommandHandler> _logger;

    public AddCommitteeMemberCommandHandler(
        ICommitteeRepository committeeRepository,
        ICurrentUserService currentUser,
        ILogger<AddCommitteeMemberCommandHandler> logger)
    {
        _committeeRepository = committeeRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(AddCommitteeMemberCommand request, CancellationToken cancellationToken)
    {
        var committee = await _committeeRepository.GetByIdWithMembersAsync(
            request.CommitteeId, cancellationToken);
        if (committee is null)
            return Result.Failure("Committee not found.");

        // ─── Conflict of Interest Validation ───
        // Get all existing committee memberships for this user in the same competition
        if (committee.CompetitionId.HasValue)
        {
            var userCommittees = await _committeeRepository.GetCommitteesByUserIdAsync(
                request.UserId, committee.CompetitionId.Value, cancellationToken);

            var existingMemberships = userCommittees
                .SelectMany(c => c.Members
                    .Where(m => m.UserId == request.UserId && m.IsActive)
                    .Select(m => (c.Type, m.Role)))
                .ToList()
                .AsReadOnly();

            var conflictResult = ConflictOfInterestRules.ValidateAssignment(
                request.UserId,
                committee.Type,
                request.Role,
                existingMemberships);

            if (conflictResult.IsFailure)
                return conflictResult;
        }

        var assignedBy = _currentUser.UserId?.ToString() ?? "system";

        var addResult = committee.AddMember(
            request.UserId,
            request.UserFullName,
            request.Role,
            request.ActiveFromPhase,
            request.ActiveToPhase,
            assignedBy);

        if (addResult.IsFailure)
            return addResult;

        _committeeRepository.Update(committee);
        await _committeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} added to committee {CommitteeId} as {Role} by {AssignedBy}",
            request.UserId, committee.Id, request.Role, assignedBy);

        return Result.Success();
    }
}
