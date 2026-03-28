using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.Committees.Commands.RemoveCommitteeMember;

/// <summary>
/// Command to remove (deactivate) a member from a committee.
/// </summary>
public sealed record RemoveCommitteeMemberCommand(
    Guid CommitteeId,
    Guid UserId,
    string Reason) : ICommand;
