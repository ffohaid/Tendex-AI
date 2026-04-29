using TendexAI.Application.Features.Rfp.Validation;

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

        RuleFor(x => x.BookletNumber)
            .MaximumLength(50)
            .WithMessage("Booklet number must not exceed 50 characters.")
            .Matches("^[A-Za-z0-9\\-/]*$")
            .WithMessage("Booklet number may contain only English letters, numbers, hyphens, and slashes.")
            .When(x => !string.IsNullOrWhiteSpace(x.BookletNumber));

        RuleFor(x => x.EstimatedBudget)
            .GreaterThan(0)
            .WithMessage("Estimated budget must be greater than zero.")
            .When(x => x.EstimatedBudget.HasValue);

        RuleFor(x => x.InquiryPeriodDays)
            .GreaterThan(0)
            .WithMessage("Inquiry period must be greater than zero days.")
            .LessThanOrEqualTo(365)
            .WithMessage("Inquiry period must not exceed 365 days.")
            .When(x => x.InquiryPeriodDays.HasValue);

        RuleFor(x => x.FiscalYear)
            .Matches("^\\d{4}$")
            .WithMessage("Fiscal year must be a four-digit year.")
            .When(x => !string.IsNullOrWhiteSpace(x.FiscalYear));

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

        RuleFor(x => x)
            .Custom((command, context) =>
            {
                var failures = CompetitionBasicInfoValidation.ValidateBasicInfoDates(
                    command.BookletIssueDate,
                    command.InquiriesStartDate,
                    command.OffersStartDate,
                    command.SubmissionDeadline,
                    command.ExpectedAwardDate,
                    command.WorkStartDate,
                    command.FiscalYear,
                    enforceBookletIssueDateInFuture: true);

                foreach (var failure in failures)
                {
                    context.AddFailure(failure.PropertyName, failure.ErrorMessage);
                }
            });
    }
}
