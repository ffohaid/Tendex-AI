namespace TendexAI.Application.Features.UserManagement.Dtos;

/// <summary>
/// DTO representing a user in list views.
/// </summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    bool IsActive,
    bool MfaEnabled,
    bool EmailConfirmed,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    IReadOnlyList<UserRoleDto> Roles);

/// <summary>
/// DTO representing a role assigned to a user.
/// </summary>
public sealed record UserRoleDto(
    Guid RoleId,
    string NameAr,
    string NameEn,
    DateTime AssignedAt,
    string? AssignedBy);

/// <summary>
/// DTO representing a role in list views.
/// </summary>
public sealed record RoleDto(
    Guid Id,
    string NameAr,
    string NameEn,
    string? Description,
    bool IsSystemRole,
    bool IsActive,
    int UserCount,
    DateTime CreatedAt);

/// <summary>
/// DTO representing an invitation in list views.
/// </summary>
public sealed record InvitationDto(
    Guid Id,
    string Email,
    string FirstNameAr,
    string LastNameAr,
    string? FirstNameEn,
    string? LastNameEn,
    string Status,
    string? RoleName,
    Guid? RoleId,
    string InvitedByName,
    DateTime ExpiresAt,
    DateTime? AcceptedAt,
    int ResendCount,
    DateTime CreatedAt);

/// <summary>
/// Paginated result wrapper for list queries.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public sealed record PaginatedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
