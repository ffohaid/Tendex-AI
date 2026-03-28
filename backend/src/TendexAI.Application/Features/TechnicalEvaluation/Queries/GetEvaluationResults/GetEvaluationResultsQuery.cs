using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetEvaluationResults;

/// <summary>
/// Query to get the final evaluation results for all offers in a competition,
/// including weighted scores, pass/fail status, and rankings.
/// </summary>
public sealed record GetEvaluationResultsQuery(
    Guid CompetitionId,
    string RequestedByUserId) : IQuery<IReadOnlyList<OfferEvaluationResultDto>>;
