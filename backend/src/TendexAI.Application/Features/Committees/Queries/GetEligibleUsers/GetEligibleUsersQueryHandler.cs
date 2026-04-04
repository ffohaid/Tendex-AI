using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Committees.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Queries.GetEligibleUsers;

/// <summary>
/// Handles the GetEligibleUsersQuery by searching for platform users
/// who are eligible to be added to a specific committee.
/// 
/// Eligibility criteria:
/// 1. User is active and belongs to the same tenant
/// 2. User is not already an active member of the committee
/// 3. If a committee role is specified, user's platform role must be compatible
/// </summary>
public sealed class GetEligibleUsersQueryHandler
    : IQueryHandler<GetEligibleUsersQuery, IReadOnlyList<EligibleUserDto>>
{
    private readonly ICommitteeRepository _committeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    /// <summary>
    /// Role compatibility matrix: maps CommitteeMemberRole to allowed SystemRole normalized names.
    /// Uses the actual NormalizedName values stored in the database Roles table.
    /// </summary>
    private static readonly Dictionary<CommitteeMemberRole, HashSet<string>> RoleCompatibilityMatrix = new()
    {
        [CommitteeMemberRole.Chair] = [
            "TENANT PRIMARY ADMIN",
            "PROCUREMENT MANAGER",
            "COMMITTEE CHAIR",
            "SECTOR REPRESENTATIVE"
        ],
        [CommitteeMemberRole.Member] = [
            "TENANT PRIMARY ADMIN",
            "PROCUREMENT MANAGER",
            "COMMITTEE CHAIR",
            "COMMITTEE MEMBER",
            "SECTOR REPRESENTATIVE",
            "FINANCIAL CONTROLLER",
            "MEMBER"
        ],
        [CommitteeMemberRole.Secretary] = [
            "TENANT PRIMARY ADMIN",
            "PROCUREMENT MANAGER",
            "COMMITTEE MEMBER",
            "SECTOR REPRESENTATIVE",
            "MEMBER"
        ]
    };

    public GetEligibleUsersQueryHandler(
        ICommitteeRepository committeeRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser)
    {
        _committeeRepository = committeeRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<EligibleUserDto>>> Handle(
        GetEligibleUsersQuery request,
        CancellationToken cancellationToken)
    {
        // Load committee to get tenant and existing members
        var committee = await _committeeRepository.GetByIdWithMembersAsync(
            request.CommitteeId, cancellationToken);
        if (committee is null)
            return Result.Failure<IReadOnlyList<EligibleUserDto>>("Committee not found.");

        // Get existing active member user IDs to exclude
        var existingMemberIds = committee.Members
            .Where(m => m.IsActive)
            .Select(m => m.UserId)
            .ToHashSet();

        // Search for users in the same tenant
        var users = await _userRepository.SearchByTenantAsync(
            committee.TenantId,
            request.SearchTerm,
            50, // fetch more to account for filtering
            cancellationToken);

        // Filter and map
        var eligibleUsers = users
            .Where(u => !existingMemberIds.Contains(u.Id))
            .Where(u =>
            {
                // If no role filter, include all users
                if (!request.Role.HasValue) return true;

                // Check role compatibility
                if (!RoleCompatibilityMatrix.TryGetValue(request.Role.Value, out var allowedRoles))
                    return true;

                var userRoleNames = u.UserRoles
                    .Select(ur => ur.Role?.NormalizedName ?? "")
                    .Where(name => !string.IsNullOrEmpty(name));

                return userRoleNames.Any(roleName => allowedRoles.Contains(roleName));
            })
            .Take(20)
            .Select(u => new EligibleUserDto(
                u.Id,
                $"{u.FirstName} {u.LastName}".Trim(),
                u.Email,
                u.UserRoles
                    .Where(ur => ur.Role is not null)
                    .Select(ur => new UserRoleSummaryDto(
                        ur.Role!.NameAr,
                        ur.Role.NameEn,
                        ur.Role.NormalizedName))
                    .ToList()
                    .AsReadOnly()))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<EligibleUserDto>>(eligibleUsers);
    }
}
