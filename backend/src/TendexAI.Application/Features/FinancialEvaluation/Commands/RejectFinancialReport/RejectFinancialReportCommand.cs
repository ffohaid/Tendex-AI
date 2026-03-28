using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.RejectFinancialReport;

public sealed record RejectFinancialReportCommand(
    Guid CompetitionId,
    string RejectedByUserId,
    string Reason) : ICommand<FinancialEvaluationDetailDto>;
