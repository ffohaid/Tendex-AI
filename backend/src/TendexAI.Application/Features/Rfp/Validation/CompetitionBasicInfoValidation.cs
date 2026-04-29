namespace TendexAI.Application.Features.Rfp.Validation;

internal static class CompetitionBasicInfoValidation
{
    public static IReadOnlyList<(string PropertyName, string ErrorMessage)> ValidateBasicInfoDates(
        DateTime? bookletIssueDate,
        DateTime? inquiriesStartDate,
        DateTime? offersStartDate,
        DateTime? submissionDeadline,
        DateTime? expectedAwardDate,
        DateTime? workStartDate,
        string? fiscalYear,
        bool enforceBookletIssueDateInFuture)
    {
        var failures = new List<(string PropertyName, string ErrorMessage)>();

        if (enforceBookletIssueDateInFuture && bookletIssueDate.HasValue && bookletIssueDate.Value.Date < DateTime.UtcNow.Date)
        {
            failures.Add((nameof(bookletIssueDate), "Booklet issue date must be today or in the future."));
        }

        if (!string.IsNullOrWhiteSpace(fiscalYear))
        {
            ValidateFiscalYear(bookletIssueDate, fiscalYear, nameof(bookletIssueDate), "Booklet issue date", failures);
            ValidateFiscalYear(inquiriesStartDate, fiscalYear, nameof(inquiriesStartDate), "Inquiries start date", failures);
            ValidateFiscalYear(offersStartDate, fiscalYear, nameof(offersStartDate), "Offers submission date", failures);
            ValidateFiscalYear(submissionDeadline, fiscalYear, nameof(submissionDeadline), "Submission deadline", failures);
            ValidateFiscalYear(expectedAwardDate, fiscalYear, nameof(expectedAwardDate), "Expected award date", failures);
            ValidateFiscalYear(workStartDate, fiscalYear, nameof(workStartDate), "Work start date", failures);
        }

        ValidateSequence(bookletIssueDate, inquiriesStartDate, nameof(inquiriesStartDate), "Inquiries start date must be on or after booklet issue date.", failures, strict: false);
        ValidateSequence(inquiriesStartDate, offersStartDate, nameof(offersStartDate), "Offers submission date must be on or after inquiries start date.", failures, strict: false);
        ValidateSequence(offersStartDate, submissionDeadline, nameof(submissionDeadline), "Submission deadline must be after offers submission date.", failures, strict: true);
        ValidateSequence(submissionDeadline, expectedAwardDate, nameof(expectedAwardDate), "Expected award date must be after submission deadline.", failures, strict: true);
        ValidateSequence(expectedAwardDate, workStartDate, nameof(workStartDate), "Work start date must be after expected award date.", failures, strict: true);

        return failures;
    }

    private static void ValidateFiscalYear(
        DateTime? value,
        string fiscalYear,
        string propertyName,
        string label,
        List<(string PropertyName, string ErrorMessage)> failures)
    {
        if (!value.HasValue)
        {
            return;
        }

        if (!int.TryParse(fiscalYear, out var fiscalYearValue) || value.Value.Year != fiscalYearValue)
        {
            failures.Add((propertyName, $"{label} must be within the selected fiscal year."));
        }
    }

    private static void ValidateSequence(
        DateTime? previous,
        DateTime? current,
        string propertyName,
        string errorMessage,
        ICollection<(string PropertyName, string ErrorMessage)> failures,
        bool strict)
    {
        if (!previous.HasValue || !current.HasValue)
        {
            return;
        }

        var isValid = strict
            ? current.Value.Date > previous.Value.Date
            : current.Value.Date >= previous.Value.Date;

        if (!isValid)
        {
            failures.Add((propertyName, errorMessage));
        }
    }
}
