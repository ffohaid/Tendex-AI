using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.SubmitTechnicalScore;

/// <summary>
/// Command for a committee member to submit a technical score
/// for a specific criterion on a specific offer.
/// Operates under blind evaluation — the evaluator sees only the blind code.
/// </summary>
public sealed record SubmitTechnicalScoreCommand(
    Guid EvaluationId,
    Guid SupplierOfferId,
    Guid EvaluationCriterionId,
    decimal Score,
    string? Notes,
    string EvaluatorUserId) : ICommand<TechnicalScoreDto>;
