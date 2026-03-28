using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.TransitionCompetition;

/// <summary>
/// Command to transition a competition to a new status.
/// Validates state machine rules, prerequisites, and permissions.
/// </summary>
public sealed record TransitionCompetitionCommand(
    Guid CompetitionId,
    CompetitionStatus TargetStatus,
    string ChangedByUserId,
    string? Reason = null) : ICommand<CompetitionTransitionResultDto>;
