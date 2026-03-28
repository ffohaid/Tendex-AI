using FluentValidation;

namespace TendexAI.Application.Features.Rfp.Commands.UpdateCompetition;

/// <summary>
/// Validates the UpdateCompetitionCommand before processing.
/// </summary>
public sealed class UpdateCompetitionCommandValidator : AbstractValidator<UpdateCompetitionCommand>
{
    public UpdateCompetitionCommandValidator()
    {
        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("Competition ID is required.");

        RuleFor(x => x.ProjectNameAr)
            .NotEmpty()
            .WithMessage("Arabic project name is required.")
            .MaximumLength(500)
            .WithMessage("Arabic project name must not exceed 500 characters.");

        RuleFor(x => x.ProjectNameEn)
            .NotEmpty()
            .WithMessage("English project name is required.")
            .MaximumLength(500)
            .WithMessage("English project name must not exceed 500 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .WithMessage("Description must not exceed 4000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.CompetitionType)
            .IsInEnum()
            .WithMessage("Invalid competition type.");

        RuleFor(x => x.EstimatedBudget)
            .GreaterThan(0)
            .WithMessage("Estimated budget must be greater than zero.")
            .When(x => x.EstimatedBudget.HasValue);

        RuleFor(x => x.SubmissionDeadline)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Submission deadline must be in the future.")
            .When(x => x.SubmissionDeadline.HasValue);

        RuleFor(x => x.ProjectDurationDays)
            .GreaterThan(0)
            .WithMessage("Project duration must be greater than zero days.")
            .LessThanOrEqualTo(3650)
            .WithMessage("Project duration must not exceed 3650 days (10 years).")
            .When(x => x.ProjectDurationDays.HasValue);

        RuleFor(x => x.ModifiedByUserId)
            .NotEmpty()
            .WithMessage("Modified by user ID is required.");
    }
}
