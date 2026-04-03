using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.UpdateCommittee;

/// <summary>
/// Command to update committee information including scope, phases, and competition links.
/// </summary>
public sealed record UpdateCommitteeCommand(
    Guid CommitteeId,
    string NameAr,
    string NameEn,
    string? Description,
    CommitteeScopeType ScopeType,
    CompetitionPhase? ActiveFromPhase,
    CompetitionPhase? ActiveToPhase,
    List<Guid>? CompetitionIds) : ICommand;
