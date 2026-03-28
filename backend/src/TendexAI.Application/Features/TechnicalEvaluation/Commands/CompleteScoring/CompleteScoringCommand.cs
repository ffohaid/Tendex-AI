using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.CompleteScoring;

/// <summary>
/// Command to finalize scoring, calculate weighted totals,
/// determine pass/fail for each offer, and submit for approval.
/// </summary>
public sealed record CompleteScoringCommand(
    Guid EvaluationId,
    string CompletedByUserId) : ICommand<IReadOnlyList<OfferEvaluationResultDto>>;
