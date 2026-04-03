using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Queries.GetPermissions;

/// <summary>
/// Handles the <see cref="GetPermissionsQuery"/>.
/// Returns all available permissions grouped by module.
/// </summary>
public sealed class GetPermissionsQueryHandler : IQueryHandler<GetPermissionsQuery, IReadOnlyList<PermissionGroupDto>>
{
    private readonly IPermissionRepository _permissionRepository;

    public GetPermissionsQueryHandler(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<Result<IReadOnlyList<PermissionGroupDto>>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);

        var groups = permissions
            .GroupBy(p => p.Module)
            .Select(g => new PermissionGroupDto(
                Module: g.Key,
                Permissions: g.Select(p => new PermissionDto(
                    Id: p.Id,
                    Code: p.Code,
                    NameAr: p.NameAr,
                    NameEn: p.NameEn,
                    Module: p.Module,
                    Description: p.Description)).ToList()))
            .OrderBy(g => g.Module)
            .ToList();

        return Result.Success<IReadOnlyList<PermissionGroupDto>>(groups);
    }
}
