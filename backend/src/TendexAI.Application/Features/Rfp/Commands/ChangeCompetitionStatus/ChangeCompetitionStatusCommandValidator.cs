using FluentValidation;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.ChangeCompetitionStatus;

/// <summary>
/// Validates the ChangeCompetitionStatusCommand before processing.
/// </summary>
public sealed class ChangeCompetitionStatusCommandValidator : AbstractValidator<ChangeCompetitionStatusCommand>
{
    public ChangeCompetitionStatusCommandValidator()
    {
        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("Competition ID is required.");

        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .WithMessage("Invalid competition status.");

        RuleFor(x => x.ChangedByUserId)
            .NotEmpty()
            .WithMessage("Changed by user ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A reason is required for rejection.")
            .MaximumLength(2000)
            .WithMessage("Reason must not exceed 2000 characters.")
            .When(x => x.NewStatus == CompetitionStatus.Rejected);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A reason is required for cancellation.")
            .MaximumLength(2000)
            .WithMessage("Reason must not exceed 2000 characters.")
            .When(x => x.NewStatus == CompetitionStatus.Cancelled);
    }
}
