using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.RejectReport;

/// <summary>
/// Command for the committee chair to reject the technical evaluation report.
/// The evaluation returns to InProgress status for re-scoring.
/// </summary>
public sealed record RejectTechnicalReportCommand(
    Guid EvaluationId,
    string RejectedByUserId,
    string Reason) : ICommand<TechnicalEvaluationDetailDto>;
