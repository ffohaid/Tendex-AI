using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.StartFinancialEvaluation;

/// <summary>
/// Command to start a financial evaluation session for a competition.
/// STRICT RULE: Technical evaluation must be approved first.
/// Per PRD Section 10.1.
/// </summary>
public sealed record StartFinancialEvaluationCommand(
    Guid CompetitionId,
    Guid CommitteeId,
    string StartedByUserId) : ICommand<FinancialEvaluationDetailDto>;
