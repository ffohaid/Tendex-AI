using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Queries.ValidateConflictOfInterest;

/// <summary>
/// Query to validate whether assigning a user to a committee would violate conflict of interest rules.
/// Returns a validation result with details.
/// </summary>
public sealed record ValidateConflictOfInterestQuery(
    Guid UserId,
    Guid CommitteeId,
    CommitteeMemberRole Role) : IQuery<ConflictOfInterestResultDto>;

/// <summary>
/// Result DTO for conflict of interest validation.
/// </summary>
public sealed record ConflictOfInterestResultDto(
    bool HasConflict,
    string? ConflictDescription,
    IReadOnlyList<string> ExistingMemberships);
