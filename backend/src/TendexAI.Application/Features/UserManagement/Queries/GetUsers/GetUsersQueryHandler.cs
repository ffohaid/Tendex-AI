using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Queries.GetUsers;

/// <summary>
/// Handles the <see cref="GetUsersQuery"/>.
/// Returns a paginated list of users for the specified tenant.
/// </summary>
public sealed class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, PaginatedResult<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<PaginatedResult<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await _userRepository.GetCountByTenantIdAsync(request.TenantId, cancellationToken);
        var users = await _userRepository.GetByTenantIdAsync(request.TenantId, request.Page, request.PageSize, cancellationToken);

        var userDtos = users.Select(u => new UserDto(
            Id: u.Id,
            Email: u.Email,
            FirstName: u.FirstName,
            LastName: u.LastName,
            PhoneNumber: u.PhoneNumber,
            IsActive: u.IsActive,
            MfaEnabled: u.MfaEnabled,
            EmailConfirmed: u.EmailConfirmed,
            LastLoginAt: u.LastLoginAt,
            CreatedAt: u.CreatedAt,
            Roles: u.UserRoles.Select(ur => new UserRoleDto(
                RoleId: ur.RoleId,
                NameAr: ur.Role.NameAr,
                NameEn: ur.Role.NameEn,
                AssignedAt: ur.AssignedAt,
                AssignedBy: ur.AssignedBy)).ToList()
        )).ToList();

        var result = new PaginatedResult<UserDto>(userDtos, totalCount, request.Page, request.PageSize);
        return Result.Success(result);
    }
}
