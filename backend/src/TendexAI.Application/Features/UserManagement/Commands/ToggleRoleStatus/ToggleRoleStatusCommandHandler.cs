using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.ToggleRoleStatus;

/// <summary>
/// Handles the <see cref="ToggleRoleStatusCommand"/>.
/// Activates or deactivates a role within the tenant.
/// </summary>
public sealed class ToggleRoleStatusCommandHandler : ICommandHandler<ToggleRoleStatusCommand>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ToggleRoleStatusCommandHandler> _logger;

    public ToggleRoleStatusCommandHandler(
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ILogger<ToggleRoleStatusCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ToggleRoleStatusCommand request, CancellationToken cancellationToken)
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

        if (role.IsSystemRole && !request.Activate)
        {
            return Result.Failure("System roles cannot be deactivated.");
        }

        if (request.Activate)
        {
            role.Activate();
        }
        else
        {
            role.Deactivate();
        }

        _roleRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role '{RoleId}' {Action} for tenant {TenantId}",
            request.RoleId, request.Activate ? "activated" : "deactivated", request.TenantId);

        return Result.Success();
    }
}
