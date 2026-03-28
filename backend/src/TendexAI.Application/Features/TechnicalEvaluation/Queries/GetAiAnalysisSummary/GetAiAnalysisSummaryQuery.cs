using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetAiAnalysisSummary;

/// <summary>
/// Query to retrieve the summary of all AI analyses for a technical evaluation.
/// </summary>
public sealed record GetAiAnalysisSummaryQuery(
    Guid EvaluationId) : IQuery<AiAnalysisSummaryDto>;
