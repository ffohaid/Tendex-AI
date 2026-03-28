using FluentValidation;
using TendexAI.Application.Features.VideoAnalysis.Commands.RecordManualReview;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.VideoAnalysis.Validators;

/// <summary>
/// Validates the RecordManualReviewCommand using FluentValidation.
/// </summary>
public sealed class RecordManualReviewCommandValidator
    : AbstractValidator<RecordManualReviewCommand>
{
    public RecordManualReviewCommandValidator()
    {
        RuleFor(x => x.AnalysisId)
            .NotEmpty()
            .WithMessage("Analysis ID is required.");

        RuleFor(x => x.ReviewerUserId)
            .NotEmpty()
            .WithMessage("Reviewer user ID is required.")
            .MaximumLength(256)
            .WithMessage("Reviewer user ID must not exceed 256 characters.");

        RuleFor(x => x.OverrideStatus)
            .Must(s => s == VideoAnalysisStatus.Passed || s == VideoAnalysisStatus.Failed)
            .WithMessage("Override status must be either Passed or Failed.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .WithMessage("Review notes must not exceed 2000 characters.")
            .When(x => x.Notes is not null);
    }
}
