using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetVarianceAlerts;

/// <summary>
/// Query to detect variance alerts between evaluators and between human/AI scores.
/// Per PRD Section 9.2 — 20% threshold.
/// </summary>
public sealed record GetVarianceAlertsQuery(
    Guid CompetitionId,
    string RequestedByUserId) : IQuery<IReadOnlyList<VarianceAlertDto>>;
