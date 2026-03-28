using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetAiOfferAnalysis;

/// <summary>
/// Query to retrieve the AI analysis for a specific offer.
/// </summary>
public sealed record GetAiOfferAnalysisQuery(
    Guid AnalysisId) : IQuery<AiOfferAnalysisDto>;
