using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.StartTechnicalEvaluation;

/// <summary>
/// Command to start a technical evaluation session for a competition.
/// Creates the TechnicalEvaluation aggregate and transitions the competition
/// to TechnicalAnalysis status if not already there.
/// </summary>
public sealed record StartTechnicalEvaluationCommand(
    Guid CompetitionId,
    Guid CommitteeId,
    string StartedByUserId) : ICommand<TechnicalEvaluationDetailDto>;
