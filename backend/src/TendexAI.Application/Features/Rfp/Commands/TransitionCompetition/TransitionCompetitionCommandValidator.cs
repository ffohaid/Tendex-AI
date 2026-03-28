using FluentValidation;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.TransitionCompetition;

/// <summary>
/// Validates the TransitionCompetitionCommand before processing.
/// </summary>
public sealed class TransitionCompetitionCommandValidator
    : AbstractValidator<TransitionCompetitionCommand>
{
    public TransitionCompetitionCommandValidator()
    {
        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("Competition ID is required.");

        RuleFor(x => x.ChangedByUserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.TargetStatus)
            .IsInEnum()
            .WithMessage("Target status must be a valid CompetitionStatus value.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(2000)
            .When(x => x.TargetStatus is CompetitionStatus.Rejected
                or CompetitionStatus.Cancelled
                or CompetitionStatus.Suspended)
            .WithMessage("A reason is required (max 2000 characters) for rejection, cancellation, or suspension.");
    }
}
