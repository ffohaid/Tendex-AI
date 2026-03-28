using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;

namespace TendexAI.Application.Features.FinancialEvaluation.Queries.GetFinancialComparisonMatrix;

public sealed record GetFinancialComparisonMatrixQuery(
    Guid CompetitionId) : IQuery<FinancialComparisonMatrixDto>;
