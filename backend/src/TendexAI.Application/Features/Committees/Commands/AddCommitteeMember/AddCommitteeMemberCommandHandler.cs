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
        _logger.LogInformation(
            "[AddMember] Starting - CommitteeId={CommitteeId}, UserId={UserId}, Role={Role}",
            request.CommitteeId, request.UserId, request.Role);

        var committee = await _committeeRepository.GetByIdWithMembersAsync(
            request.CommitteeId, cancellationToken);
        if (committee is null)
        {
            _logger.LogWarning("[AddMember] Committee {CommitteeId} not found", request.CommitteeId);
            return Result.Failure("Committee not found.");
        }

        _logger.LogInformation(
            "[AddMember] Committee loaded - Id={Id}, Status={Status}, MemberCount={MemberCount}",
            committee.Id, committee.Status, committee.Members.Count);

        // ─── Conflict of Interest Validation ───
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
            {
                _logger.LogWarning("[AddMember] Conflict of interest: {Error}", conflictResult.Error);
                return conflictResult;
            }
        }

        var assignedBy = _currentUser.UserId?.ToString() ?? "system";

        _logger.LogInformation("[AddMember] Calling committee.AddMember with assignedBy={AssignedBy}", assignedBy);

        var addResult = committee.AddMember(
            request.UserId,
            request.UserFullName,
            request.Role,
            request.ActiveFromPhase,
            request.ActiveToPhase,
            assignedBy);

        if (addResult.IsFailure)
        {
            _logger.LogWarning("[AddMember] AddMember domain failed: {Error}", addResult.Error);
            return addResult;
        }

        _logger.LogInformation(
            "[AddMember] Domain AddMember succeeded. MemberCount now={MemberCount}. Calling SaveChangesAsync...",
            committee.Members.Count);

        try
        {
            await _committeeRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("[AddMember] SaveChangesAsync succeeded!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AddMember] SaveChangesAsync FAILED. ExceptionType={ExType}, Message={Message}",
                ex.GetType().Name, ex.Message);
            throw;
        }

        _logger.LogInformation(
            "User {UserId} added to committee {CommitteeId} as {Role} by {AssignedBy}",
            request.UserId, committee.Id, request.Role, assignedBy);

        return Result.Success();
    }
}
