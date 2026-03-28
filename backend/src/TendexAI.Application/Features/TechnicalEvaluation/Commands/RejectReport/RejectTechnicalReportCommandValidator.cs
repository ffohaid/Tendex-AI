using FluentValidation;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.RejectReport;

public sealed class RejectTechnicalReportCommandValidator
    : AbstractValidator<RejectTechnicalReportCommand>
{
    public RejectTechnicalReportCommandValidator()
    {
        RuleFor(x => x.EvaluationId)
            .NotEmpty()
            .WithMessage("Evaluation ID is required.");

        RuleFor(x => x.RejectedByUserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A rejection reason is required.")
            .MaximumLength(2000)
            .WithMessage("Rejection reason must not exceed 2000 characters.");
    }
}
