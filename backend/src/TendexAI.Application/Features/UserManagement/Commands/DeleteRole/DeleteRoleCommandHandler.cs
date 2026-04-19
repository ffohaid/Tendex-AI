using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.DeleteRole;

/// <summary>
/// Handles the <see cref="DeleteRoleCommand"/>.
/// Deletes a custom (non-system, non-protected) role that has no assigned users.
/// </summary>
public sealed class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<DeleteRoleCommandHandler> _logger;

    public DeleteRoleCommandHandler(
        IRoleRepository roleRepository,
        ILogger<DeleteRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            _logger.LogWarning("Delete role failed: role {RoleId} not found", request.RoleId);
            return Result.Failure("Role not found.");
        }

        if (role.TenantId != request.TenantId)
        {
            _logger.LogWarning("Delete role failed: tenant mismatch for role {RoleId}", request.RoleId);
            return Result.Failure("Role does not belong to the current tenant.");
        }

        if (role.IsSystemRole)
        {
            _logger.LogWarning("Delete role failed: cannot delete system role {RoleId}", request.RoleId);
            return Result.Failure("System roles cannot be deleted.");
        }

        if (role.IsProtected)
        {
            _logger.LogWarning("Delete role failed: cannot delete protected role {RoleId}", request.RoleId);
            return Result.Failure("Protected governance roles cannot be deleted.");
        }

        if (role.UserRoles.Count > 0)
        {
            _logger.LogWarning("Delete role failed: role {RoleId} has {Count} assigned users", request.RoleId, role.UserRoles.Count);
            return Result.Failure($"Cannot delete role with {role.UserRoles.Count} assigned user(s). Remove all users from this role first.");
        }

        // Clear permissions first, then delete the role
        await _roleRepository.UpdatePermissionsAsync(role.Id, Array.Empty<Guid>(), cancellationToken);

        // Use EF Core to remove the role entity
        // We need a Delete method on the repository
        _roleRepository.Delete(role);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role {RoleId} ({RoleName}) deleted by tenant {TenantId}",
            request.RoleId, role.NameEn, request.TenantId);

        return Result.Success();
    }
}
