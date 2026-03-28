using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetAiComparisonMatrix;

/// <summary>
/// Query to retrieve the AI comparison matrix across all offers and criteria
/// for a technical evaluation. Enables the committee to compare AI-suggested
/// scores side-by-side.
/// </summary>
public sealed record GetAiComparisonMatrixQuery(
    Guid EvaluationId) : IQuery<AiComparisonMatrixDto>;
