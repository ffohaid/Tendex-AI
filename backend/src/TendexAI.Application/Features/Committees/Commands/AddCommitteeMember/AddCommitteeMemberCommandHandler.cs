using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.AddCommitteeMember;

/// <summary>
/// Handles adding a registered platform user as a member to a committee.
/// Validates:
/// 1. User exists in the platform and is active
/// 2. User belongs to the same tenant
/// 3. User's platform role is compatible with the requested committee role
/// 4. Conflict of interest rules (PRD Section 4.2, Section 23 Rules #3, #7)
/// Members inherit the committee's phase scope — no per-member phase override.
/// </summary>
public sealed class AddCommitteeMemberCommandHandler : ICommandHandler<AddCommitteeMemberCommand>
{
    private readonly ICommitteeRepository _committeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AddCommitteeMemberCommandHandler> _logger;

    /// <summary>
    /// Role compatibility matrix: maps CommitteeMemberRole to allowed SystemRole values.
    /// Chair: Owner, Admin, SectorRep
    /// Member: Owner, Admin, SectorRep, FinancialController, Member
    /// Secretary: Admin, SectorRep, Member
    /// </summary>
    private static readonly Dictionary<CommitteeMemberRole, HashSet<string>> RoleCompatibilityMatrix = new()
    {
        [CommitteeMemberRole.Chair] = ["TENANT PRIMARY ADMIN", "PROCUREMENT MANAGER", "SECTOR REPRESENTATIVE", "COMMITTEE CHAIR"],
        [CommitteeMemberRole.Member] = ["TENANT PRIMARY ADMIN", "PROCUREMENT MANAGER", "SECTOR REPRESENTATIVE", "FINANCIAL CONTROLLER", "COMMITTEE MEMBER", "COMMITTEE CHAIR", "MEMBER"],
        [CommitteeMemberRole.Secretary] = ["TENANT PRIMARY ADMIN", "PROCUREMENT MANAGER", "SECTOR REPRESENTATIVE", "MEMBER", "COMMITTEE MEMBER"]
    };

    public AddCommitteeMemberCommandHandler(
        ICommitteeRepository committeeRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        ILogger<AddCommitteeMemberCommandHandler> logger)
    {
        _committeeRepository = committeeRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result> Handle(AddCommitteeMemberCommand request, CancellationToken cancellationToken)
    {
        // ─── Load Committee ───────────────────────────────────────────────
        var committee = await _committeeRepository.GetByIdWithMembersAsync(
            request.CommitteeId, cancellationToken);
        if (committee is null)
            return Result.Failure("Committee not found.");

        // ─── Validate User Exists and Is Active ──────────────────────────
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure("User not found. Only registered platform users can be added to committees.");

        if (!user.IsActive)
            return Result.Failure("User account is not active. Only active users can be assigned to committees.");

        // ─── Validate Same Tenant ────────────────────────────────────────
        if (user.TenantId != committee.TenantId)
            return Result.Failure("User does not belong to the same organization as the committee.");

        // ─── Validate Role Compatibility ─────────────────────────────────
        var userRoleNames = user.UserRoles
            .Select(ur => ur.Role?.NormalizedName ?? "")
            .Where(name => !string.IsNullOrEmpty(name))
            .ToHashSet();

        if (RoleCompatibilityMatrix.TryGetValue(request.Role, out var allowedRoles))
        {
            var isCompatible = userRoleNames.Any(roleName => allowedRoles.Contains(roleName));
            if (!isCompatible)
            {
                var allowedRolesStr = string.Join(", ", allowedRoles);
                return Result.Failure(
                    $"User's platform role is not compatible with the committee role '{request.Role}'. " +
                    $"Allowed platform roles: {allowedRolesStr}.");
            }
        }

        // ─── Conflict of Interest Validation ─────────────────────────────
        var competitionIds = committee.Competitions
            .Select(c => c.CompetitionId)
            .ToList();

        if (competitionIds.Count > 0)
        {
            foreach (var competitionId in competitionIds)
            {
                var userCommittees = await _committeeRepository.GetCommitteesByUserIdAsync(
                    request.UserId, competitionId, cancellationToken);

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
        }

        // ─── Add Member (no per-member phase override) ──────────────────
        var assignedBy = _currentUser.UserId?.ToString() ?? "system";
        var userFullName = $"{user.FirstName} {user.LastName}".Trim();

        var addResult = committee.AddMember(
            request.UserId,
            userFullName,
            request.Role,
            assignedBy);

        if (addResult.IsFailure)
            return addResult;

        await _committeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} ({UserName}) added to committee {CommitteeId} as {Role} by {AssignedBy}",
            request.UserId, userFullName, committee.Id, request.Role, assignedBy);

        return Result.Success();
    }
}
