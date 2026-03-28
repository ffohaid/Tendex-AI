using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.ApproveReport;

/// <summary>
/// Command for the committee chair to approve the technical evaluation report.
/// Upon approval:
/// - Blind evaluation is deactivated (supplier identities revealed).
/// - Financial envelopes are unlocked for passed offers only.
/// - Competition can transition to FinancialAnalysis phase.
/// Per PRD Section 9.1 and 10.1.
/// </summary>
public sealed record ApproveTechnicalReportCommand(
    Guid EvaluationId,
    string ApprovedByUserId) : ICommand<TechnicalEvaluationDetailDto>;
