using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.SubmitFinancialScore;

/// <summary>
/// Command to submit a financial evaluation score for a supplier offer.
/// Per PRD Section 10.2.
/// </summary>
public sealed record SubmitFinancialScoreCommand(
    Guid CompetitionId,
    Guid SupplierOfferId,
    string EvaluatorUserId,
    decimal Score,
    decimal MaxScore,
    string? Notes) : ICommand<FinancialScoreDto>;
