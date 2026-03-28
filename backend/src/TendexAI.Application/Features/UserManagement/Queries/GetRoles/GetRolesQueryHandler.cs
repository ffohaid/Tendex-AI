using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Queries.GetRoles;

/// <summary>
/// Handles the <see cref="GetRolesQuery"/>.
/// Returns all roles for the specified tenant.
/// </summary>
public sealed class GetRolesQueryHandler : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IRoleRepository _roleRepository;

    public GetRolesQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.GetByTenantIdAsync(request.TenantId, cancellationToken);

        var roleDtos = roles.Select(r => new RoleDto(
            Id: r.Id,
            NameAr: r.NameAr,
            NameEn: r.NameEn,
            Description: r.Description,
            IsSystemRole: r.IsSystemRole,
            IsActive: r.IsActive,
            UserCount: r.UserRoles.Count,
            CreatedAt: r.CreatedAt)).ToList();

        return Result.Success<IReadOnlyList<RoleDto>>(roleDtos);
    }
}
