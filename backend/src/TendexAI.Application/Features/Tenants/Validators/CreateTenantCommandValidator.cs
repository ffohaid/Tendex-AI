using FluentValidation;
using TendexAI.Application.Features.Tenants.Commands.CreateTenant;

namespace TendexAI.Application.Features.Tenants.Validators;

/// <summary>
/// Validator for CreateTenantCommand.
/// Ensures all required fields meet business rules before processing.
/// </summary>
public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic name is required.")
            .MaximumLength(256).WithMessage("Arabic name must not exceed 256 characters.");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English name is required.")
            .MaximumLength(256).WithMessage("English name must not exceed 256 characters.");

        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("Identifier is required.")
            .MaximumLength(50).WithMessage("Identifier must not exceed 50 characters.")
            .Matches(@"^[A-Za-z][A-Za-z0-9_-]*$")
            .WithMessage("Identifier must start with a letter and contain only letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomain is required.")
            .MaximumLength(100).WithMessage("Subdomain must not exceed 100 characters.")
            .Matches(@"^[a-z][a-z0-9-]*$")
            .WithMessage("Subdomain must start with a lowercase letter and contain only lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.ContactPersonEmail)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactPersonEmail))
            .WithMessage("Contact person email must be a valid email address.");

        RuleFor(x => x.ContactPersonPhone)
            .MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.ContactPersonPhone))
            .WithMessage("Contact person phone must not exceed 20 characters.");

        RuleFor(x => x.PrimaryColor)
            .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$")
            .When(x => !string.IsNullOrWhiteSpace(x.PrimaryColor))
            .WithMessage("Primary color must be a valid hex color code (e.g., #FF5733).");

        RuleFor(x => x.SecondaryColor)
            .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$")
            .When(x => !string.IsNullOrWhiteSpace(x.SecondaryColor))
            .WithMessage("Secondary color must be a valid hex color code (e.g., #FF5733).");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Notes))
            .WithMessage("Notes must not exceed 2000 characters.");
    }
}
