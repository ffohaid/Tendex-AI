using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.AddCommitteeMember;

/// <summary>
/// Command to add a registered platform user as a member to a committee.
/// The user must exist in the platform and their role must be compatible
/// with the requested committee role.
/// </summary>
public sealed record AddCommitteeMemberCommand(
    Guid CommitteeId,
    Guid UserId,
    CommitteeMemberRole Role,
    CompetitionPhase? ActiveFromPhase,
    CompetitionPhase? ActiveToPhase) : ICommand;
