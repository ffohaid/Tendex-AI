using FluentValidation;

namespace TendexAI.Application.Features.UserManagement.Commands.CreateRole;

/// <summary>
/// Validates the <see cref="CreateRoleCommand"/>.
/// </summary>
public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic role name is required.")
            .MaximumLength(100).WithMessage("Arabic role name must not exceed 100 characters.");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English role name is required.")
            .MaximumLength(100).WithMessage("English role name must not exceed 100 characters.");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");
    }
}
