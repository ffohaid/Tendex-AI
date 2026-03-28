using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.VideoAnalysis.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.VideoAnalysis.Commands.RecordManualReview;

/// <summary>
/// Handles the RecordManualReviewCommand by updating the analysis with a human review decision.
/// </summary>
public sealed class RecordManualReviewCommandHandler
    : ICommandHandler<RecordManualReviewCommand, VideoIntegrityAnalysisDto>
{
    private readonly IVideoIntegrityAnalysisRepository _repository;
    private readonly ILogger<RecordManualReviewCommandHandler> _logger;

    public RecordManualReviewCommandHandler(
        IVideoIntegrityAnalysisRepository repository,
        ILogger<RecordManualReviewCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<VideoIntegrityAnalysisDto>> Handle(
        RecordManualReviewCommand request,
        CancellationToken cancellationToken)
    {
        var analysis = await _repository.GetByIdWithFlagsAsync(
            request.AnalysisId, cancellationToken);

        if (analysis is null)
            return Result.Failure<VideoIntegrityAnalysisDto>("Video integrity analysis not found.");

        try
        {
            analysis.RecordManualReview(
                request.ReviewerUserId,
                request.OverrideStatus,
                request.Notes);

            await _repository.UpdateAsync(analysis, cancellationToken);

            _logger.LogInformation(
                "Manual review recorded for analysis {AnalysisId} by {ReviewerId}, status set to {Status}",
                analysis.Id, request.ReviewerUserId, request.OverrideStatus);

            return Result.Success(analysis.ToDto());
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<VideoIntegrityAnalysisDto>(ex.Message);
        }
    }
}
