using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.CreateCommittee;

/// <summary>
/// Command to create a new committee (permanent or temporary).
/// </summary>
public sealed record CreateCommitteeCommand(
    string NameAr,
    string NameEn,
    CommitteeType Type,
    bool IsPermanent,
    CommitteeScopeType ScopeType,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    List<Guid>? CompetitionIds,
    CompetitionPhase? ActiveFromPhase,
    CompetitionPhase? ActiveToPhase) : ICommand<Guid>;
