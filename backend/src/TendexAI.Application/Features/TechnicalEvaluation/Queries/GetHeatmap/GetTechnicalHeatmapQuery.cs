using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetHeatmap;

/// <summary>
/// Query to generate the technical evaluation heatmap for a competition.
/// Per PRD Section 9.3 — color-coded comparison matrix.
/// </summary>
public sealed record GetTechnicalHeatmapQuery(
    Guid CompetitionId,
    string RequestedByUserId) : IQuery<TechnicalHeatmapDto>;
