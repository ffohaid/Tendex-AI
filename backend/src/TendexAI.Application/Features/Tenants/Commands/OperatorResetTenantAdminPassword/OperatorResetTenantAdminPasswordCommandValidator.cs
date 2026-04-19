using FluentValidation;

namespace TendexAI.Application.Features.Tenants.Commands.OperatorResetTenantAdminPassword;

/// <summary>
/// FluentValidation validator for <see cref="OperatorResetTenantAdminPasswordCommand"/>.
/// Enforces password strength requirements and input integrity.
/// </summary>
public sealed class OperatorResetTenantAdminPasswordCommandValidator
    : AbstractValidator<OperatorResetTenantAdminPasswordCommand>
{
    public OperatorResetTenantAdminPasswordCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("معرّف الجهة مطلوب.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("كلمة المرور الجديدة مطلوبة.")
            .MinimumLength(8)
            .WithMessage("كلمة المرور يجب أن تحتوي على 8 أحرف على الأقل.")
            .Matches("[A-Z]")
            .WithMessage("كلمة المرور يجب أن تحتوي على حرف كبير واحد على الأقل.")
            .Matches("[a-z]")
            .WithMessage("كلمة المرور يجب أن تحتوي على حرف صغير واحد على الأقل.")
            .Matches("[0-9]")
            .WithMessage("كلمة المرور يجب أن تحتوي على رقم واحد على الأقل.")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage("كلمة المرور يجب أن تحتوي على رمز خاص واحد على الأقل.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("تأكيد كلمة المرور مطلوب.")
            .Equal(x => x.NewPassword)
            .WithMessage("كلمة المرور الجديدة وتأكيدها غير متطابقتين.");

        RuleFor(x => x.OperatorUserId)
            .NotEmpty()
            .WithMessage("معرّف المشغل مطلوب.");

        RuleFor(x => x.OperatorName)
            .NotEmpty()
            .WithMessage("اسم المشغل مطلوب.");
    }
}
