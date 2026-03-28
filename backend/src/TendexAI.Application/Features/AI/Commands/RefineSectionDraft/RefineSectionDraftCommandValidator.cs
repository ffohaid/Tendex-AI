using FluentValidation;

namespace TendexAI.Application.Features.AI.Commands.RefineSectionDraft;

/// <summary>
/// Validates the RefineSectionDraftCommand before processing.
/// </summary>
public sealed class RefineSectionDraftCommandValidator
    : AbstractValidator<RefineSectionDraftCommand>
{
    public RefineSectionDraftCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("CompetitionId is required.");

        RuleFor(x => x.ProjectNameAr)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("ProjectNameAr is required and must not exceed 500 characters.");

        RuleFor(x => x.SectionTitleAr)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("SectionTitleAr is required and must not exceed 500 characters.");

        RuleFor(x => x.CurrentContentHtml)
            .NotEmpty()
            .WithMessage("CurrentContentHtml is required — the existing draft content must be provided.");

        RuleFor(x => x.UserFeedbackAr)
            .NotEmpty()
            .MaximumLength(5000)
            .WithMessage("UserFeedbackAr is required and must not exceed 5000 characters.");

        RuleFor(x => x.CollectionName)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("CollectionName is required and must not exceed 200 characters.");
    }
}
