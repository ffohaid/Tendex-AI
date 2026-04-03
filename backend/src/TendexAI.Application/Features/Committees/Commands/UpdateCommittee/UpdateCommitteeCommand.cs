using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.UpdateCommittee;

/// <summary>
/// Command to update committee information including scope, phases, and competition links.
/// Phases is a list of selected competition phases (for non-comprehensive scope types).
/// </summary>
public sealed record UpdateCommitteeCommand(
    Guid CommitteeId,
    string NameAr,
    string NameEn,
    string? Description,
    CommitteeScopeType ScopeType,
    List<CompetitionPhase>? Phases,
    List<Guid>? CompetitionIds) : ICommand;
