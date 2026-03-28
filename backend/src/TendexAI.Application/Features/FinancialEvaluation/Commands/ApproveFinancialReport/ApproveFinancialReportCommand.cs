using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.ApproveFinancialReport;

public sealed record ApproveFinancialReportCommand(
    Guid CompetitionId,
    string ApprovedByUserId) : ICommand<FinancialEvaluationDetailDto>;
