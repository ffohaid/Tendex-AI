using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Queries.GetRoleById;

/// <summary>
/// Handles the <see cref="GetRoleByIdQuery"/>.
/// Returns a role with its permissions.
/// </summary>
public sealed class GetRoleByIdQueryHandler : IQueryHandler<GetRoleByIdQuery, RoleDetailDto>
{
    private readonly IRoleRepository _roleRepository;

    public GetRoleByIdQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Result<RoleDetailDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null || role.TenantId != request.TenantId)
        {
            return Result.Failure<RoleDetailDto>("Role not found.");
        }

        var permissions = role.RolePermissions.Select(rp => new PermissionDto(
            Id: rp.Permission.Id,
            Code: rp.Permission.Code,
            NameAr: rp.Permission.NameAr,
            NameEn: rp.Permission.NameEn,
            Module: rp.Permission.Module,
            Description: rp.Permission.Description)).ToList();

        var users = role.UserRoles.Select(ur => new RoleUserDto(
            UserId: ur.UserId,
            AssignedAt: ur.AssignedAt,
            AssignedBy: ur.AssignedBy)).ToList();

        var dto = new RoleDetailDto(
            Id: role.Id,
            NameAr: role.NameAr,
            NameEn: role.NameEn,
            Description: role.Description,
            IsSystemRole: role.IsSystemRole,
            IsActive: role.IsActive,
            UserCount: role.UserRoles.Count,
            CreatedAt: role.CreatedAt,
            Permissions: permissions,
            Users: users);

        return Result.Success(dto);
    }
}
