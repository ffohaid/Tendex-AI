using FluentValidation;

namespace TendexAI.Application.Features.Impersonation.Commands.RejectConsent;

/// <summary>
/// Validates the <see cref="RejectImpersonationConsentCommand"/>.
/// </summary>
public sealed class RejectImpersonationConsentCommandValidator
    : AbstractValidator<RejectImpersonationConsentCommand>
{
    public RejectImpersonationConsentCommandValidator()
    {
        RuleFor(x => x.ConsentId)
            .NotEmpty()
            .WithMessage("Consent ID is required.");

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .WithMessage("A rejection reason is required.")
            .MaximumLength(2000)
            .WithMessage("Rejection reason must not exceed 2000 characters.");
    }
}
