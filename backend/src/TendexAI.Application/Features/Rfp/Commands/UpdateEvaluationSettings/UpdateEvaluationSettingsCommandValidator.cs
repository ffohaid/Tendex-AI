using FluentValidation;

namespace TendexAI.Application.Features.Rfp.Commands.UpdateEvaluationSettings;

/// <summary>
/// Validates the UpdateEvaluationSettingsCommand before processing.
/// </summary>
public sealed class UpdateEvaluationSettingsCommandValidator : AbstractValidator<UpdateEvaluationSettingsCommand>
{
    public UpdateEvaluationSettingsCommandValidator()
    {
        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("Competition ID is required.");

        RuleFor(x => x.ModifiedByUserId)
            .NotEmpty()
            .WithMessage("Modified by user ID is required.");

        RuleFor(x => x.TechnicalPassingScore)
            .InclusiveBetween(0, 100)
            .WithMessage("Technical passing score must be between 0 and 100.")
            .When(x => x.TechnicalPassingScore.HasValue);

        RuleFor(x => x.TechnicalWeight)
            .InclusiveBetween(0, 100)
            .WithMessage("Technical weight must be between 0 and 100.")
            .When(x => x.TechnicalWeight.HasValue);

        RuleFor(x => x.FinancialWeight)
            .InclusiveBetween(0, 100)
            .WithMessage("Financial weight must be between 0 and 100.")
            .When(x => x.FinancialWeight.HasValue);

        RuleFor(x => x)
            .Must(x => !x.TechnicalWeight.HasValue || !x.FinancialWeight.HasValue ||
                        x.TechnicalWeight.Value + x.FinancialWeight.Value == 100m)
            .WithMessage("Technical and financial weights must sum to 100%.")
            .When(x => x.TechnicalWeight.HasValue && x.FinancialWeight.HasValue);
    }
}
