using FluentValidation;

namespace TendexAI.Application.Features.UserManagement.Commands.UpdateRole;

/// <summary>
/// Validates the <see cref="UpdateRoleCommand"/>.
/// </summary>
public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");

        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic role name is required.")
            .MaximumLength(100).WithMessage("Arabic role name must not exceed 100 characters.");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English role name is required.")
            .MaximumLength(100).WithMessage("English role name must not exceed 100 characters.");

        RuleFor(x => x.DescriptionAr)
            .MaximumLength(500).WithMessage("Arabic description must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.DescriptionAr));

        RuleFor(x => x.DescriptionEn)
            .MaximumLength(500).WithMessage("English description must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.DescriptionEn));

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");
    }
}
