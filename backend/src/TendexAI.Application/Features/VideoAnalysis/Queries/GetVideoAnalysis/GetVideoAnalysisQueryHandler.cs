using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.VideoAnalysis.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.VideoAnalysis.Queries.GetVideoAnalysis;

/// <summary>
/// Handles the GetVideoAnalysisQuery by retrieving a single analysis record.
/// </summary>
public sealed class GetVideoAnalysisQueryHandler
    : IQueryHandler<GetVideoAnalysisQuery, VideoIntegrityAnalysisDto>
{
    private readonly IVideoIntegrityAnalysisRepository _repository;

    public GetVideoAnalysisQueryHandler(IVideoIntegrityAnalysisRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<VideoIntegrityAnalysisDto>> Handle(
        GetVideoAnalysisQuery request,
        CancellationToken cancellationToken)
    {
        var analysis = await _repository.GetByIdWithFlagsAsync(
            request.AnalysisId, cancellationToken);

        if (analysis is null)
            return Result.Failure<VideoIntegrityAnalysisDto>("Video integrity analysis not found.");

        return Result.Success(analysis.ToDto());
    }
}
