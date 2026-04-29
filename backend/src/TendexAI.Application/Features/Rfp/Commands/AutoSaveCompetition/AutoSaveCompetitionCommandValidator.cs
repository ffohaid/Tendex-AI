using FluentValidation;
using TendexAI.Application.Features.Rfp.Validation;

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

        RuleFor(x => x.BookletNumber)
            .MaximumLength(50)
            .WithMessage("Booklet number must not exceed 50 characters.")
            .Matches("^[A-Za-z0-9\\-/]*$")
            .WithMessage("Booklet number may contain only English letters, numbers, hyphens, and slashes.")
            .When(x => !string.IsNullOrWhiteSpace(x.BookletNumber));

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

        RuleFor(x => x.CurrentWizardStep)
            .InclusiveBetween(1, 6)
            .WithMessage("Wizard step must be between 1 and 6.")
            .When(x => x.CurrentWizardStep.HasValue);

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
                    enforceBookletIssueDateInFuture: false);

                foreach (var failure in failures)
                {
                    context.AddFailure(failure.PropertyName, failure.ErrorMessage);
                }
            });
    }
}
