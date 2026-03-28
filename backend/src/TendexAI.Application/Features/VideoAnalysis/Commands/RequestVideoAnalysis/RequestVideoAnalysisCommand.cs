using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.VideoAnalysis.Dtos;

namespace TendexAI.Application.Features.VideoAnalysis.Commands.RequestVideoAnalysis;

/// <summary>
/// Command to request a new video integrity analysis.
/// This creates the analysis record and triggers the AI analysis pipeline.
/// </summary>
public sealed record RequestVideoAnalysisCommand(
    Guid TenantId,
    Guid CompetitionId,
    Guid? SupplierOfferId,
    string VideoFileReference,
    string ExpectedUserId,
    string? VideoFileName,
    long? VideoFileSizeBytes,
    TimeSpan? VideoDuration,
    string? ReferenceImageUrl) : ICommand<VideoIntegrityAnalysisDto>;
