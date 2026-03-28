using FluentValidation;
using TendexAI.Application.Features.Auth.Commands;

namespace TendexAI.Application.Features.Auth.Validators;

/// <summary>
/// Validates the <see cref="ForgotPasswordCommand"/> input.
/// </summary>
public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("البريد الإلكتروني مطلوب.")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة.")
            .MaximumLength(256).WithMessage("البريد الإلكتروني يجب ألا يتجاوز 256 حرفاً.");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("معرف الجهة مطلوب.");
    }
}
