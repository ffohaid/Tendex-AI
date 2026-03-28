using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Queries.GetUserById;

/// <summary>
/// Handles the <see cref="GetUserByIdQuery"/>.
/// Returns a single user by their ID.
/// </summary>
public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<UserDto>("User not found.");

        if (user.TenantId != request.TenantId)
            return Result.Failure<UserDto>("User does not belong to the current tenant.");

        var userDto = new UserDto(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            IsActive: user.IsActive,
            MfaEnabled: user.MfaEnabled,
            EmailConfirmed: user.EmailConfirmed,
            LastLoginAt: user.LastLoginAt,
            CreatedAt: user.CreatedAt,
            Roles: user.UserRoles.Select(ur => new UserRoleDto(
                RoleId: ur.RoleId,
                NameAr: ur.Role.NameAr,
                NameEn: ur.Role.NameEn,
                AssignedAt: ur.AssignedAt,
                AssignedBy: ur.AssignedBy)).ToList());

        return Result.Success(userDto);
    }
}
