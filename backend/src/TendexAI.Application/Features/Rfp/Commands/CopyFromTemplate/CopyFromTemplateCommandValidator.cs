using FluentValidation;
using System.Globalization;

namespace TendexAI.Application.Features.Rfp.Commands.CopyFromTemplate;

public sealed class CopyFromTemplateCommandValidator : AbstractValidator<CopyFromTemplateCommand>
{
    public CopyFromTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty()
            .WithMessage("Template ID is required.");

        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.ProjectNameAr)
            .NotEmpty()
            .WithMessage("Arabic project name is required.")
            .MaximumLength(500)
            .WithMessage("Arabic project name must not exceed 500 characters.");

        RuleFor(x => x.ProjectNameEn)
            .MaximumLength(500)
            .WithMessage("English project name must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ProjectNameEn));

        RuleFor(x => x.DescriptionAr)
            .NotEmpty()
            .WithMessage("Project description is required.")
            .MaximumLength(4000)
            .WithMessage("Project description must not exceed 4000 characters.");

        RuleFor(x => x.CompetitionType)
            .IsInEnum()
            .WithMessage("Invalid competition type.");

        RuleFor(x => x.EstimatedBudget)
            .NotNull()
            .WithMessage("Estimated budget is required.")
            .GreaterThan(0)
            .WithMessage("Estimated budget must be greater than zero.");

        RuleFor(x => x.BookletNumber)
            .MaximumLength(50)
            .WithMessage("Booklet number must not exceed 50 characters.")
            .Matches(@"^[A-Za-z0-9\-/]*$")
            .WithMessage("Booklet number may contain only English letters, numbers, hyphens, and slashes.")
            .When(x => !string.IsNullOrWhiteSpace(x.BookletNumber));

        RuleFor(x => x.Department)
            .NotEmpty()
            .WithMessage("Department is required.")
            .MaximumLength(200)
            .WithMessage("Department must not exceed 200 characters.");

        RuleFor(x => x.FiscalYear)
            .NotEmpty()
            .WithMessage("Fiscal year is required.")
            .Matches(@"^\d{4}$")
            .WithMessage("Fiscal year must be a four-digit year.");

        RuleFor(x => x.InquiryPeriodDays)
            .GreaterThan(0)
            .WithMessage("Inquiry period must be greater than zero days.")
            .LessThanOrEqualTo(365)
            .WithMessage("Inquiry period must not exceed 365 days.")
            .When(x => x.InquiryPeriodDays.HasValue);

        RuleFor(x => x)
            .Custom((command, context) =>
            {
                ValidateFiscalYearDates(command, context);
            });
    }

    private static void ValidateFiscalYearDates(CopyFromTemplateCommand command, ValidationContext<CopyFromTemplateCommand> context)
    {
        var fiscalYear = command.FiscalYear?.Trim();
        if (string.IsNullOrWhiteSpace(fiscalYear) || fiscalYear.Length != 4)
        {
            return;
        }

        var orderedDates = new (string PropertyName, DateTime? Value, string DisplayName, bool MustBeAfterPrevious)[]
        {
            (nameof(command.BookletIssueDate), command.BookletIssueDate, "Booklet issue date", false),
            (nameof(command.InquiriesStartDate), command.InquiriesStartDate, "Inquiries start date", false),
            (nameof(command.OffersStartDate), command.OffersStartDate, "Offers start date", false),
            (nameof(command.SubmissionDeadline), command.SubmissionDeadline, "Submission deadline", true),
            (nameof(command.ExpectedAwardDate), command.ExpectedAwardDate, "Expected award date", true),
            (nameof(command.WorkStartDate), command.WorkStartDate, "Work start date", true),
        };

        foreach (var item in orderedDates)
        {
            if (!item.Value.HasValue)
            {
                continue;
            }

            if (item.Value.Value.Year.ToString(CultureInfo.InvariantCulture) != fiscalYear)
            {
                context.AddFailure(item.PropertyName, $"{item.DisplayName} must be within the selected fiscal year.");
                return;
            }
        }

        if (command.BookletIssueDate.HasValue && command.BookletIssueDate.Value.Date < DateTime.UtcNow.Date)
        {
            context.AddFailure(nameof(command.BookletIssueDate), "Booklet issue date must be today or later.");
            return;
        }

        for (var index = 1; index < orderedDates.Length; index++)
        {
            var current = orderedDates[index];
            var previous = orderedDates[index - 1];
            if (!current.Value.HasValue || !previous.Value.HasValue)
            {
                continue;
            }

            var isValid = current.MustBeAfterPrevious
                ? current.Value.Value.Date > previous.Value.Value.Date
                : current.Value.Value.Date >= previous.Value.Value.Date;

            if (!isValid)
            {
                var relation = current.MustBeAfterPrevious ? "after" : "on or after";
                context.AddFailure(current.PropertyName, $"{current.DisplayName} must be {relation} {previous.DisplayName.ToLowerInvariant()}.");
                return;
            }
        }
    }
}
