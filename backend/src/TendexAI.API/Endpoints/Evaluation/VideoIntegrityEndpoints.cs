using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.VideoAnalysis.Commands.RecordManualReview;
using TendexAI.Application.Features.VideoAnalysis.Commands.RequestVideoAnalysis;
using TendexAI.Application.Features.VideoAnalysis.Dtos;
using TendexAI.Application.Features.VideoAnalysis.Queries.GetVideoAnalysis;
using TendexAI.Application.Features.VideoAnalysis.Queries.GetVideoAnalysesByCompetition;
using TendexAI.Domain.Enums;

namespace TendexAI.API.Endpoints.Evaluation;

/// <summary>
/// Defines Minimal API endpoints for video integrity analysis.
/// These endpoints are completely separate from the core evaluation endpoints
/// to ensure no impact on the existing scoring pipeline.
/// </summary>
public static class VideoIntegrityEndpoints
{
    /// <summary>
    /// Maps all video integrity analysis endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapVideoIntegrityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/video-integrity")
            .WithTags("Video Integrity Analysis")
            .RequireAuthorization();

        // ═══════════════════════════════════════════════════════════
        //  Analysis Request
        // ═══════════════════════════════════════════════════════════

        group.MapPost("/analyze", RequestAnalysisAsync)
            .WithName("RequestVideoIntegrityAnalysis")
            .WithSummary("Request a new video integrity analysis")
            .WithDescription(
                "Submits a video for AI-driven integrity analysis including tamper detection, " +
                "screen re-recording detection, and identity verification. " +
                "This operation is asynchronous; the analysis runs in the background.")
            .Produces<VideoIntegrityAnalysisDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity);

        // ═══════════════════════════════════════════════════════════
        //  Query Endpoints
        // ═══════════════════════════════════════════════════════════

        group.MapGet("/{analysisId:guid}", GetAnalysisAsync)
            .WithName("GetVideoIntegrityAnalysis")
            .WithSummary("Get a specific video integrity analysis by ID")
            .Produces<VideoIntegrityAnalysisDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("/competition/{competitionId:guid}", GetByCompetitionAsync)
            .WithName("GetVideoAnalysesByCompetition")
            .WithSummary("Get all video integrity analyses for a competition")
            .Produces<IReadOnlyList<VideoIntegrityAnalysisDto>>(StatusCodes.Status200OK);

        // ═══════════════════════════════════════════════════════════
        //  Manual Review
        // ═══════════════════════════════════════════════════════════

        group.MapPost("/{analysisId:guid}/review", RecordManualReviewAsync)
            .WithName("RecordVideoAnalysisManualReview")
            .WithSummary("Record a manual review decision on a video analysis")
            .WithDescription(
                "Used when an analysis result is inconclusive and requires human judgment. " +
                "The reviewer can override the status to Passed or Failed.")
            .Produces<VideoIntegrityAnalysisDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return app;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler Methods
    // ═══════════════════════════════════════════════════════════════

    private static async Task<IResult> RequestAnalysisAsync(
        [FromBody] RequestVideoAnalysisRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RequestVideoAnalysisCommand(
            TenantId: request.TenantId,
            CompetitionId: request.CompetitionId,
            SupplierOfferId: request.SupplierOfferId,
            VideoFileReference: request.VideoFileReference,
            ExpectedUserId: request.ExpectedUserId,
            VideoFileName: request.VideoFileName,
            VideoFileSizeBytes: request.VideoFileSizeBytes,
            VideoDuration: request.VideoDuration,
            ReferenceImageUrl: request.ReferenceImageUrl);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status422UnprocessableEntity);

        return Results.Created(
            $"/api/v1/video-integrity/{result.Value!.Id}",
            result.Value);
    }

    private static async Task<IResult> GetAnalysisAsync(
        [FromRoute] Guid analysisId,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetVideoAnalysisQuery(analysisId);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound);

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetByCompetitionAsync(
        [FromRoute] Guid competitionId,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetVideoAnalysesByCompetitionQuery(competitionId);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> RecordManualReviewAsync(
        [FromRoute] Guid analysisId,
        [FromBody] RecordManualReviewRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RecordManualReviewCommand(
            AnalysisId: analysisId,
            ReviewerUserId: request.ReviewerUserId,
            OverrideStatus: request.OverrideStatus,
            Notes: request.Notes);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Ok(result.Value);
    }
}

// ═══════════════════════════════════════════════════════════════
//  Request Models
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Request model for submitting a video integrity analysis.
/// </summary>
public sealed record RequestVideoAnalysisRequest
{
    public Guid TenantId { get; init; }
    public Guid CompetitionId { get; init; }
    public Guid? SupplierOfferId { get; init; }
    public string VideoFileReference { get; init; } = null!;
    public string ExpectedUserId { get; init; } = null!;
    public string? VideoFileName { get; init; }
    public long? VideoFileSizeBytes { get; init; }
    public TimeSpan? VideoDuration { get; init; }
    public string? ReferenceImageUrl { get; init; }
}

/// <summary>
/// Request model for recording a manual review decision.
/// </summary>
public sealed record RecordManualReviewRequest
{
    public string ReviewerUserId { get; init; } = null!;
    public VideoAnalysisStatus OverrideStatus { get; init; }
    public string? Notes { get; init; }
}
