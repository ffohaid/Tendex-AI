using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.VideoAnalysis.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.VideoAnalysis.Commands.RequestVideoAnalysis;

/// <summary>
/// Handles the RequestVideoAnalysisCommand by creating a new analysis record
/// and delegating to the IVideoIntegrityService for AI processing.
/// This handler is completely decoupled from the core evaluation pipeline.
/// </summary>
public sealed class RequestVideoAnalysisCommandHandler
    : ICommandHandler<RequestVideoAnalysisCommand, VideoIntegrityAnalysisDto>
{
    private readonly IVideoIntegrityAnalysisRepository _repository;
    private readonly IVideoIntegrityService _videoIntegrityService;
    private readonly ILogger<RequestVideoAnalysisCommandHandler> _logger;

    public RequestVideoAnalysisCommandHandler(
        IVideoIntegrityAnalysisRepository repository,
        IVideoIntegrityService videoIntegrityService,
        ILogger<RequestVideoAnalysisCommandHandler> logger)
    {
        _repository = repository;
        _videoIntegrityService = videoIntegrityService;
        _logger = logger;
    }

    public async Task<Result<VideoIntegrityAnalysisDto>> Handle(
        RequestVideoAnalysisCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Requesting video integrity analysis for competition {CompetitionId}, video {VideoRef}",
            request.CompetitionId, request.VideoFileReference);

        // 1. Check if analysis already exists for this video
        var existing = await _repository.GetLatestByVideoReferenceAsync(
            request.VideoFileReference, cancellationToken);

        if (existing is not null &&
            existing.Status != Domain.Enums.VideoAnalysisStatus.Error)
        {
            _logger.LogInformation(
                "Analysis already exists for video {VideoRef} with status {Status}",
                request.VideoFileReference, existing.Status);
            return Result.Success(existing.ToDto());
        }

        // 2. Create new analysis record
        var analysis = new VideoIntegrityAnalysis(
            tenantId: request.TenantId,
            competitionId: request.CompetitionId,
            supplierOfferId: request.SupplierOfferId,
            videoFileReference: request.VideoFileReference,
            expectedUserId: request.ExpectedUserId,
            videoFileName: request.VideoFileName,
            videoFileSizeBytes: request.VideoFileSizeBytes,
            videoDuration: request.VideoDuration);

        await _repository.AddAsync(analysis, cancellationToken);

        // 3. Execute AI analysis pipeline
        try
        {
            analysis.StartAnalysis();
            var result = await _videoIntegrityService.AnalyzeVideoAsync(
                analysis,
                request.ReferenceImageUrl,
                cancellationToken);

            await _repository.UpdateAsync(result, cancellationToken);

            _logger.LogInformation(
                "Video integrity analysis {AnalysisId} completed with status {Status}, confidence {Confidence}",
                result.Id, result.Status, result.OverallConfidenceScore);

            return Result.Success(result.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Video integrity analysis failed for {AnalysisId}",
                analysis.Id);

            analysis.FailAnalysis($"Analysis pipeline error: {ex.Message}");
            await _repository.UpdateAsync(analysis, cancellationToken);

            return Result.Success(analysis.ToDto());
        }
    }
}
