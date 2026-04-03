using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Committees.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Queries.GetEligibleUsers;

/// <summary>
/// Query to search for platform users eligible to be added to a committee.
/// Filters by: active status, same tenant, role compatibility, not already a member.
/// </summary>
public sealed record GetEligibleUsersQuery(
    Guid CommitteeId,
    CommitteeMemberRole? Role,
    string? SearchTerm) : IQuery<IReadOnlyList<EligibleUserDto>>;
