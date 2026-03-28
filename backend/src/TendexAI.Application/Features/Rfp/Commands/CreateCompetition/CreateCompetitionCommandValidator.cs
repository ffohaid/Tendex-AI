using FluentValidation;

namespace TendexAI.Application.Features.Rfp.Commands.CreateCompetition;

/// <summary>
/// Validates the CreateCompetitionCommand before processing.
/// </summary>
public sealed class CreateCompetitionCommandValidator : AbstractValidator<CreateCompetitionCommand>
{
    public CreateCompetitionCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required.");

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

        RuleFor(x => x.CreationMethod)
            .IsInEnum()
            .WithMessage("Invalid creation method.");

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

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty()
            .WithMessage("Created by user ID is required.");

        RuleFor(x => x.SourceTemplateId)
            .NotEmpty()
            .WithMessage("Source template ID must be valid when provided.")
            .When(x => x.SourceTemplateId.HasValue);

        RuleFor(x => x.SourceCompetitionId)
            .NotEmpty()
            .WithMessage("Source competition ID must be valid when provided.")
            .When(x => x.SourceCompetitionId.HasValue);
    }
}
