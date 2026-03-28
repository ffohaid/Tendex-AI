using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.ChangeCompetitionStatus;

/// <summary>
/// Command to change the status of a competition (submit, approve, reject, cancel).
/// </summary>
public sealed record ChangeCompetitionStatusCommand(
    Guid CompetitionId,
    CompetitionStatus NewStatus,
    string? Reason,
    string ChangedByUserId) : ICommand<CompetitionDetailDto>;
