namespace TendexAI.Application.Features.Profile.Dtos;

/// <summary>
/// DTO representing the current user's profile information.
/// </summary>
public sealed record ProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? AvatarUrl,
    bool MfaEnabled,
    bool EmailConfirmed,
    DateTime? LastLoginAt,
    string? LastLoginIp,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    Guid TenantId,
    string? TenantName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastModifiedAt);

/// <summary>
/// Request DTO for updating user profile.
/// </summary>
public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Email);

/// <summary>
/// Request DTO for changing password.
/// </summary>
public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);
