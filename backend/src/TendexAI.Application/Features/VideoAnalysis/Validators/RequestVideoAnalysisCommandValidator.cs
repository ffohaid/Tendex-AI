using FluentValidation;
using TendexAI.Application.Features.VideoAnalysis.Commands.RequestVideoAnalysis;

namespace TendexAI.Application.Features.VideoAnalysis.Validators;

/// <summary>
/// Validates the RequestVideoAnalysisCommand using FluentValidation.
/// Ensures all required fields are present and valid before processing.
/// </summary>
public sealed class RequestVideoAnalysisCommandValidator
    : AbstractValidator<RequestVideoAnalysisCommand>
{
    /// <summary>
    /// Maximum allowed video file size: 500 MB.
    /// </summary>
    private const long MaxVideoFileSizeBytes = 500L * 1024 * 1024;

    /// <summary>
    /// Maximum allowed video duration: 30 minutes.
    /// </summary>
    private static readonly TimeSpan MaxVideoDuration = TimeSpan.FromMinutes(30);

    public RequestVideoAnalysisCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required.");

        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("Competition ID is required.");

        RuleFor(x => x.VideoFileReference)
            .NotEmpty()
            .WithMessage("Video file reference is required.")
            .MaximumLength(1024)
            .WithMessage("Video file reference must not exceed 1024 characters.");

        RuleFor(x => x.ExpectedUserId)
            .NotEmpty()
            .WithMessage("Expected user ID is required.")
            .MaximumLength(256)
            .WithMessage("Expected user ID must not exceed 256 characters.");

        RuleFor(x => x.VideoFileName)
            .MaximumLength(512)
            .WithMessage("Video file name must not exceed 512 characters.")
            .When(x => x.VideoFileName is not null);

        RuleFor(x => x.VideoFileSizeBytes)
            .GreaterThan(0)
            .WithMessage("Video file size must be greater than zero.")
            .LessThanOrEqualTo(MaxVideoFileSizeBytes)
            .WithMessage($"Video file size must not exceed {MaxVideoFileSizeBytes / (1024 * 1024)} MB.")
            .When(x => x.VideoFileSizeBytes.HasValue);

        RuleFor(x => x.VideoDuration)
            .LessThanOrEqualTo(MaxVideoDuration)
            .WithMessage($"Video duration must not exceed {MaxVideoDuration.TotalMinutes} minutes.")
            .When(x => x.VideoDuration.HasValue);

        RuleFor(x => x.ReferenceImageUrl)
            .MaximumLength(2048)
            .WithMessage("Reference image URL must not exceed 2048 characters.")
            .When(x => x.ReferenceImageUrl is not null);
    }
}
