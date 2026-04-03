using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.CreateRole;

/// <summary>
/// Handles the <see cref="CreateRoleCommand"/>.
/// Creates a new custom role within the tenant.
/// </summary>
public sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleDto>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateRoleCommandHandler> _logger;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.NameEn.ToUpperInvariant();

        // Check for duplicate role name
        if (await _roleRepository.ExistsByNameAsync(normalizedName, request.TenantId, cancellationToken))
        {
            return Result.Failure<RoleDto>("A role with this name already exists.");
        }

        var role = new Role(
            request.NameAr,
            request.NameEn,
            normalizedName,
            request.TenantId,
            isSystemRole: false);

        // Set description
        var description = !string.IsNullOrWhiteSpace(request.DescriptionAr)
            ? request.DescriptionAr
            : request.DescriptionEn;
        if (!string.IsNullOrWhiteSpace(description))
        {
            role.SetDescription(description);
        }

        // Add permissions if provided
        if (request.PermissionIds is { Count: > 0 })
        {
            foreach (var permissionId in request.PermissionIds)
            {
                var rolePermission = new RolePermission(role.Id, permissionId);
                role.RolePermissions.Add(rolePermission);
            }
        }

        await _roleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created custom role '{RoleName}' for tenant {TenantId}", request.NameEn, request.TenantId);

        return Result.Success(new RoleDto(
            Id: role.Id,
            NameAr: role.NameAr,
            NameEn: role.NameEn,
            Description: role.Description,
            IsSystemRole: role.IsSystemRole,
            IsActive: role.IsActive,
            UserCount: 0,
            CreatedAt: role.CreatedAt));
    }
}
