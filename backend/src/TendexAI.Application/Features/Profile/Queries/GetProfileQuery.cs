using MediatR;
using TendexAI.Application.Features.Profile.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.Profile.Queries;

/// <summary>
/// Query to retrieve the current user's profile.
/// </summary>
public sealed record GetProfileQuery(Guid UserId) : IRequest<Result<ProfileDto>>;

/// <summary>
/// Handles retrieval of the current user's profile information.
/// </summary>
public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
{
    private readonly IUserRepository _userRepository;

    public GetProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<ProfileDto>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<ProfileDto>("User not found.");

        var roles = user.UserRoles
            .Where(ur => ur.Role is not null)
            .Select(ur => ur.Role!.NameEn)
            .ToList();

        var permissions = user.UserRoles
            .Where(ur => ur.Role?.RolePermissions is not null)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Where(rp => rp.Permission is not null)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToList();

        var dto = new ProfileDto(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            AvatarUrl: user.AvatarUrl,
            MfaEnabled: user.MfaEnabled,
            EmailConfirmed: user.EmailConfirmed,
            LastLoginAt: user.LastLoginAt,
            LastLoginIp: user.LastLoginIp,
            Roles: roles,
            Permissions: permissions,
            TenantId: user.TenantId,
            TenantName: null, // Resolved at API layer if needed
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt,
            LastModifiedAt: user.LastModifiedAt);

        return Result.Success(dto);
    }
}
