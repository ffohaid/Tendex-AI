using FluentValidation;

namespace TendexAI.Application.Features.Committees.Commands.RemoveCommitteeMember;

/// <summary>
/// Validates the RemoveCommitteeMemberCommand input.
/// </summary>
public sealed class RemoveCommitteeMemberCommandValidator : AbstractValidator<RemoveCommitteeMemberCommand>
{
    public RemoveCommitteeMemberCommandValidator()
    {
        RuleFor(x => x.CommitteeId)
            .NotEmpty().WithMessage("Committee ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required for removing a committee member.")
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.");
    }
}
