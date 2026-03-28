using FluentValidation;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.ChangeCommitteeStatus;

/// <summary>
/// Validates the ChangeCommitteeStatusCommand input.
/// </summary>
public sealed class ChangeCommitteeStatusCommandValidator : AbstractValidator<ChangeCommitteeStatusCommand>
{
    public ChangeCommitteeStatusCommandValidator()
    {
        RuleFor(x => x.CommitteeId)
            .NotEmpty().WithMessage("Committee ID is required.");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid committee status.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .When(x => x.NewStatus is CommitteeStatus.Suspended or CommitteeStatus.Dissolved)
            .WithMessage("A reason is required when suspending or dissolving a committee.")
            .MaximumLength(2000).WithMessage("Reason must not exceed 2000 characters.");
    }
}
