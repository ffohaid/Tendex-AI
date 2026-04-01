using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetTechnicalScores;

/// <summary>
/// Query to get all technical scores for a competition's evaluation.
/// </summary>
public sealed record GetTechnicalScoresQuery(
    Guid CompetitionId,
    string RequestedByUserId) : IQuery<IReadOnlyList<TechnicalScoreDto>>;
