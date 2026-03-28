using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.VideoAnalysis.Dtos;

namespace TendexAI.Application.Features.VideoAnalysis.Queries.GetVideoAnalysis;

/// <summary>
/// Query to retrieve a single video integrity analysis by its ID.
/// </summary>
public sealed record GetVideoAnalysisQuery(Guid AnalysisId) : IQuery<VideoIntegrityAnalysisDto>;
