using FluentValidation;
using TendexAI.Application.Features.Auth.Commands;

namespace TendexAI.Application.Features.Auth.Validators;

/// <summary>
/// Validates the <see cref="LoginCommand"/> input.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");

        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("IP address is required.");
    }
}

/// <summary>
/// Validates the <see cref="RefreshTokenCommand"/> input.
/// </summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");

        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("IP address is required.");
    }
}

/// <summary>
/// Validates the <see cref="VerifyMfaCommand"/> input.
/// </summary>
public sealed class VerifyMfaCommandValidator : AbstractValidator<VerifyMfaCommand>
{
    public VerifyMfaCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("Verification code must contain only digits.");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");

        RuleFor(x => x.IpAddress)
            .NotEmpty().WithMessage("IP address is required.");
    }
}

/// <summary>
/// Validates the <see cref="DisableMfaCommand"/> input.
/// </summary>
public sealed class DisableMfaCommandValidator : AbstractValidator<DisableMfaCommand>
{
    public DisableMfaCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("Verification code must contain only digits.");
    }
}
