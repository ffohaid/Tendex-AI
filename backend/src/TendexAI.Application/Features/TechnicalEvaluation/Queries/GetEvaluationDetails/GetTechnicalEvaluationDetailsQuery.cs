using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetEvaluationDetails;

/// <summary>
/// Query to get the full details of a technical evaluation session
/// including all scores and offer results.
/// </summary>
public sealed record GetTechnicalEvaluationDetailsQuery(
    Guid CompetitionId,
    string RequestedByUserId) : IQuery<TechnicalEvaluationDetailDto>;
