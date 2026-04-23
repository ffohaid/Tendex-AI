using FluentValidation;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.StartTechnicalEvaluation;

public sealed class StartTechnicalEvaluationCommandValidator
    : AbstractValidator<StartTechnicalEvaluationCommand>
{
    public StartTechnicalEvaluationCommandValidator()
    {
        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("Competition ID is required.");

        RuleFor(x => x.CommitteeId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Committee ID must be a valid GUID if provided.");

        RuleFor(x => x.StartedByUserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}
