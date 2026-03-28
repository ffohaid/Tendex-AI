using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;

namespace TendexAI.Application.Features.FinancialEvaluation.Queries.GetFinancialEvaluationDetails;

public sealed record GetFinancialEvaluationDetailsQuery(
    Guid CompetitionId) : IQuery<FinancialEvaluationDetailDto>;
