using FluentValidation;

namespace TendexAI.Application.Features.UserManagement.Commands.AcceptInvitation;

/// <summary>
/// Validates the <see cref="AcceptInvitationCommand"/> input using FluentValidation.
/// </summary>
public sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Invitation token is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z\d]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required.")
            .Equal(x => x.Password).WithMessage("Password confirmation does not match.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?\d{9,15}$").WithMessage("Phone number must be between 9 and 15 digits.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
