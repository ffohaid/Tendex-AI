using FluentValidation;

namespace TendexAI.Application.Features.Committees.Commands.AddCommitteeMember;

/// <summary>
/// Validates the AddCommitteeMemberCommand input.
/// </summary>
public sealed class AddCommitteeMemberCommandValidator : AbstractValidator<AddCommitteeMemberCommand>
{
    public AddCommitteeMemberCommandValidator()
    {
        RuleFor(x => x.CommitteeId)
            .NotEmpty().WithMessage("Committee ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.UserFullName)
            .NotEmpty().WithMessage("User full name is required.")
            .MaximumLength(300).WithMessage("User full name must not exceed 300 characters.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid committee member role.");
    }
}
