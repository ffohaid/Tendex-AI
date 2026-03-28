using FluentValidation;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.SubmitTechnicalScore;

public sealed class SubmitTechnicalScoreCommandValidator
    : AbstractValidator<SubmitTechnicalScoreCommand>
{
    public SubmitTechnicalScoreCommandValidator()
    {
        RuleFor(x => x.EvaluationId)
            .NotEmpty()
            .WithMessage("Evaluation ID is required.");

        RuleFor(x => x.SupplierOfferId)
            .NotEmpty()
            .WithMessage("Supplier offer ID is required.");

        RuleFor(x => x.EvaluationCriterionId)
            .NotEmpty()
            .WithMessage("Evaluation criterion ID is required.");

        RuleFor(x => x.Score)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Score must be non-negative.");

        RuleFor(x => x.EvaluatorUserId)
            .NotEmpty()
            .WithMessage("Evaluator user ID is required.");
    }
}
