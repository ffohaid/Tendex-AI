using FluentValidation;

namespace TendexAI.Application.Features.Rfp.Commands.AutoSaveCompetition;

/// <summary>
/// Lightweight validation for auto-save command.
/// Only validates essential fields; content validation is relaxed for drafts.
/// </summary>
public sealed class AutoSaveCompetitionCommandValidator : AbstractValidator<AutoSaveCompetitionCommand>
{
    public AutoSaveCompetitionCommandValidator()
    {
        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("Competition ID is required.");

        RuleFor(x => x.ModifiedByUserId)
            .NotEmpty()
            .WithMessage("Modified by user ID is required.");

        RuleFor(x => x.ProjectNameAr)
            .MaximumLength(500)
            .WithMessage("Arabic project name must not exceed 500 characters.")
            .When(x => x.ProjectNameAr is not null);

        RuleFor(x => x.ProjectNameEn)
            .MaximumLength(500)
            .WithMessage("English project name must not exceed 500 characters.")
            .When(x => x.ProjectNameEn is not null);

        RuleFor(x => x.CurrentWizardStep)
            .InclusiveBetween(1, 6)
            .WithMessage("Wizard step must be between 1 and 6.")
            .When(x => x.CurrentWizardStep.HasValue);
    }
}
