using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.VideoAnalysis.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.VideoAnalysis.Queries.GetVideoAnalysesByCompetition;

/// <summary>
/// Handles the GetVideoAnalysesByCompetitionQuery by retrieving all analyses for a competition.
/// </summary>
public sealed class GetVideoAnalysesByCompetitionQueryHandler
    : IQueryHandler<GetVideoAnalysesByCompetitionQuery, IReadOnlyList<VideoIntegrityAnalysisDto>>
{
    private readonly IVideoIntegrityAnalysisRepository _repository;

    public GetVideoAnalysesByCompetitionQueryHandler(
        IVideoIntegrityAnalysisRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<VideoIntegrityAnalysisDto>>> Handle(
        GetVideoAnalysesByCompetitionQuery request,
        CancellationToken cancellationToken)
    {
        var analyses = await _repository.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        var dtos = analyses.Select(a => a.ToDto()).ToList();

        return Result.Success<IReadOnlyList<VideoIntegrityAnalysisDto>>(dtos);
    }
}
