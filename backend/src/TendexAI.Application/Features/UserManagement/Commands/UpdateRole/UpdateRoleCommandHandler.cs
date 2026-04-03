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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateRoleCommandHandler> _logger;

    public UpdateRoleCommandHandler(
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
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
            // Remove existing permissions
            role.RolePermissions.Clear();

            // Add new permissions
            foreach (var permissionId in request.PermissionIds)
            {
                var rolePermission = new RolePermission(role.Id, permissionId);
                role.RolePermissions.Add(rolePermission);
            }
        }

        _roleRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated role '{RoleId}' for tenant {TenantId}", request.RoleId, request.TenantId);

        return Result.Success();
    }
}
