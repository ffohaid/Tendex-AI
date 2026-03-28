using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.VideoAnalysis.Dtos;

namespace TendexAI.Application.Features.VideoAnalysis.Queries.GetVideoAnalysesByCompetition;

/// <summary>
/// Query to retrieve all video integrity analyses for a specific competition.
/// </summary>
public sealed record GetVideoAnalysesByCompetitionQuery(
    Guid CompetitionId) : IQuery<IReadOnlyList<VideoIntegrityAnalysisDto>>;
