using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.CompleteFinancialScoring;

public sealed record CompleteFinancialScoringCommand(
    Guid CompetitionId,
    string CompletedByUserId) : ICommand<FinancialEvaluationDetailDto>;
