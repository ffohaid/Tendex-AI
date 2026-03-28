using FluentValidation;

namespace TendexAI.Application.Features.Rfp.Commands.AddEvaluationCriterion;

/// <summary>
/// Validates the AddEvaluationCriterionCommand before processing.
/// </summary>
public sealed class AddEvaluationCriterionCommandValidator : AbstractValidator<AddEvaluationCriterionCommand>
{
    public AddEvaluationCriterionCommandValidator()
    {
        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("Competition ID is required.");

        RuleFor(x => x.NameAr)
            .NotEmpty()
            .WithMessage("Arabic criterion name is required.")
            .MaximumLength(500)
            .WithMessage("Arabic criterion name must not exceed 500 characters.");

        RuleFor(x => x.NameEn)
            .NotEmpty()
            .WithMessage("English criterion name is required.")
            .MaximumLength(500)
            .WithMessage("English criterion name must not exceed 500 characters.");

        RuleFor(x => x.DescriptionAr)
            .MaximumLength(2000)
            .WithMessage("Arabic description must not exceed 2000 characters.")
            .When(x => x.DescriptionAr is not null);

        RuleFor(x => x.DescriptionEn)
            .MaximumLength(2000)
            .WithMessage("English description must not exceed 2000 characters.")
            .When(x => x.DescriptionEn is not null);

        RuleFor(x => x.WeightPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Weight percentage must be between 0 and 100.");

        RuleFor(x => x.MinimumPassingScore)
            .InclusiveBetween(0, 100)
            .WithMessage("Minimum passing score must be between 0 and 100.")
            .When(x => x.MinimumPassingScore.HasValue);

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty()
            .WithMessage("Created by user ID is required.");
    }
}
