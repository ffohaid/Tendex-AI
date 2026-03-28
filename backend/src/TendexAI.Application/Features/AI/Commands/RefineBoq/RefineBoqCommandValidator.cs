using FluentValidation;

namespace TendexAI.Application.Features.AI.Commands.RefineBoq;

/// <summary>
/// Validates the RefineBoqCommand before processing.
/// </summary>
public sealed class RefineBoqCommandValidator
    : AbstractValidator<RefineBoqCommand>
{
    public RefineBoqCommandValidator()
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

        RuleFor(x => x.ExistingBoqJson)
            .NotEmpty()
            .WithMessage("ExistingBoqJson is required — the current BOQ data must be provided.");

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
