using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.UpdateRole;

/// <summary>
/// Handles the <see cref="UpdateRoleCommand"/>.
/// Updates role name, description, and permissions.
/// </summary>
public sealed class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<UpdateRoleCommandHandler> _logger;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        ILogger<UpdateRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure("Role not found.");
        }

        if (role.TenantId != request.TenantId)
        {
            return Result.Failure("Role does not belong to the current tenant.");
        }

        // Protected roles (OperatorPrimaryAdmin, TenantPrimaryAdmin) cannot be modified
        if (role.IsProtected)
        {
            return Result.Failure("Protected governance roles cannot be modified.");
        }

        // System roles can only have their description updated
        if (!role.IsSystemRole)
        {
            // Check for duplicate name (excluding current role)
            var normalizedName = request.NameEn.ToUpperInvariant();
            var existing = await _roleRepository.GetByNameAsync(normalizedName, request.TenantId, cancellationToken);
            if (existing is not null && existing.Id != role.Id)
            {
                return Result.Failure("A role with this name already exists.");
            }

            role.UpdateName(request.NameAr, request.NameEn);
        }

        role.SetDescription(request.Description);

        // Update permissions if provided
        if (request.PermissionIds is not null)
        {
            // Use the repository method to handle permission updates properly
            // This avoids EF Core concurrency issues with Clear() + re-add
            await _roleRepository.UpdatePermissionsAsync(role.Id, request.PermissionIds, cancellationToken);
        }

        // CRITICAL FIX: Use repository's SaveChangesAsync which operates on TenantDbContext
        await _roleRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated role '{RoleId}' for tenant {TenantId}", request.RoleId, request.TenantId);

        return Result.Success();
    }
}
