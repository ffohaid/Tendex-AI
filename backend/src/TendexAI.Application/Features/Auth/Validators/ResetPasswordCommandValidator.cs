using FluentValidation;
using TendexAI.Application.Features.Auth.Commands;

namespace TendexAI.Application.Features.Auth.Validators;

/// <summary>
/// Validates the <see cref="ResetPasswordCommand"/> input.
/// </summary>
public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("معرف الجلسة مطلوب.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("رمز إعادة التعيين مطلوب.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("كلمة المرور الجديدة مطلوبة.")
            .MinimumLength(8).WithMessage("كلمة المرور يجب أن تحتوي على 8 أحرف على الأقل.")
            .MaximumLength(128).WithMessage("كلمة المرور يجب ألا تتجاوز 128 حرفاً.")
            .Matches("[A-Z]").WithMessage("كلمة المرور يجب أن تحتوي على حرف كبير واحد على الأقل.")
            .Matches("[a-z]").WithMessage("كلمة المرور يجب أن تحتوي على حرف صغير واحد على الأقل.")
            .Matches("[0-9]").WithMessage("كلمة المرور يجب أن تحتوي على رقم واحد على الأقل.")
            .Matches("[^a-zA-Z0-9]").WithMessage("كلمة المرور يجب أن تحتوي على رمز خاص واحد على الأقل.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("تأكيد كلمة المرور مطلوب.")
            .Equal(x => x.NewPassword).WithMessage("كلمة المرور الجديدة وتأكيدها غير متطابقتين.");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("معرف الجهة مطلوب.");
    }
}
