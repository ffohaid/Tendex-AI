using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.ChangeCommitteeStatus;

/// <summary>
/// Command to change the lifecycle status of a committee (Suspend, Reactivate, Dissolve).
/// </summary>
public sealed record ChangeCommitteeStatusCommand(
    Guid CommitteeId,
    CommitteeStatus NewStatus,
    string? Reason) : ICommand;
