using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.AddCommitteeMember;

/// <summary>
/// Command to add a member to a committee with conflict of interest validation.
/// </summary>
public sealed record AddCommitteeMemberCommand(
    Guid CommitteeId,
    Guid UserId,
    string UserFullName,
    CommitteeMemberRole Role,
    CompetitionPhase? ActiveFromPhase,
    CompetitionPhase? ActiveToPhase) : ICommand;
