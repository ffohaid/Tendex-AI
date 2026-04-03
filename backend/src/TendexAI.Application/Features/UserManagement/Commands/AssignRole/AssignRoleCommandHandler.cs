using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.AssignRole;

/// <summary>
/// Handles the <see cref="AssignRoleCommand"/>.
/// Assigns a role to a user within the same tenant.
/// </summary>
public sealed class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<AssignRoleCommandHandler> _logger;

    public AssignRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ILogger<AssignRoleCommandHandler> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure("User not found.");

        if (user.TenantId != request.TenantId)
            return Result.Failure("User does not belong to the current tenant.");

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure("Role not found.");

        if (role.TenantId != request.TenantId)
            return Result.Failure("Role does not belong to the current tenant.");

        if (!role.IsActive)
            return Result.Failure("Cannot assign an inactive role.");

        var hasRole = await _userRepository.HasRoleAsync(request.UserId, request.RoleId, cancellationToken);
        if (hasRole)
            return Result.Failure("User already has this role assigned.");

        var userRole = new UserRole(request.UserId, request.RoleId, request.AssignedBy);
        await _userRepository.AddUserRoleAsync(userRole, cancellationToken);
        // CRITICAL FIX: Use repository's SaveChangesAsync which operates on TenantDbContext
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role {RoleId} assigned to user {UserId} by {AssignedBy}",
            request.RoleId, request.UserId, request.AssignedBy);

        return Result.Success();
    }
}
