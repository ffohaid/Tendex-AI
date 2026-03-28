using FluentValidation;

namespace TendexAI.Application.Features.UserManagement.Commands.AssignRole;

/// <summary>
/// Validates the <see cref="AssignRoleCommand"/> input using FluentValidation.
/// </summary>
public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID is required.");

        RuleFor(x => x.AssignedBy)
            .NotEmpty().WithMessage("Assigned by is required.");
    }
}
