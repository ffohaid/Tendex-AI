using FluentValidation;

namespace TendexAI.Application.Features.Impersonation.Commands.RequestConsent;

/// <summary>
/// Validates the <see cref="RequestImpersonationConsentCommand"/>.
/// </summary>
public sealed class RequestImpersonationConsentCommandValidator
    : AbstractValidator<RequestImpersonationConsentCommand>
{
    public RequestImpersonationConsentCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty()
            .WithMessage("Target user ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A reason for impersonation is required.")
            .MaximumLength(2000)
            .WithMessage("Reason must not exceed 2000 characters.")
            .MinimumLength(10)
            .WithMessage("Reason must be at least 10 characters long.");

        RuleFor(x => x.TicketReference)
            .MaximumLength(256)
            .WithMessage("Ticket reference must not exceed 256 characters.");
    }
}
