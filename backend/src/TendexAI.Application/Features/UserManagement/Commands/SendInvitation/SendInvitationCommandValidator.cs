using FluentValidation;

namespace TendexAI.Application.Features.UserManagement.Commands.SendInvitation;

/// <summary>
/// Validates the <see cref="SendInvitationCommand"/> input using FluentValidation.
/// </summary>
public sealed class SendInvitationCommandValidator : AbstractValidator<SendInvitationCommand>
{
    public SendInvitationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.FirstNameAr)
            .NotEmpty().WithMessage("Arabic first name is required.")
            .MaximumLength(100).WithMessage("Arabic first name must not exceed 100 characters.")
            .Matches(@"[\u0600-\u06FF\s]+").WithMessage("Arabic first name must contain Arabic characters.");

        RuleFor(x => x.LastNameAr)
            .NotEmpty().WithMessage("Arabic last name is required.")
            .MaximumLength(100).WithMessage("Arabic last name must not exceed 100 characters.")
            .Matches(@"[\u0600-\u06FF\s]+").WithMessage("Arabic last name must contain Arabic characters.");

        RuleFor(x => x.FirstNameEn)
            .MaximumLength(100).WithMessage("English first name must not exceed 100 characters.")
            .Matches(@"^[a-zA-Z\s]*$").WithMessage("English first name must contain only English characters.")
            .When(x => !string.IsNullOrEmpty(x.FirstNameEn));

        RuleFor(x => x.LastNameEn)
            .MaximumLength(100).WithMessage("English last name must not exceed 100 characters.")
            .Matches(@"^[a-zA-Z\s]*$").WithMessage("English last name must contain only English characters.")
            .When(x => !string.IsNullOrEmpty(x.LastNameEn));

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");

        RuleFor(x => x.InvitedByUserId)
            .NotEmpty().WithMessage("Inviter user ID is required.");

        RuleFor(x => x.BaseUrl)
            .NotEmpty().WithMessage("Base URL is required.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Base URL must be a valid absolute URL.");
    }
}
