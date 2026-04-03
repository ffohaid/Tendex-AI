namespace TendexAI.API.Endpoints.UserManagement;

/// <summary>
/// Request model for updating user profile information.
/// </summary>
public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber);

/// <summary>
/// Request model for activating/deactivating a user.
/// </summary>
public sealed record ToggleUserStatusRequest(bool Activate);

/// <summary>
/// Request model for assigning a role to a user.
/// </summary>
public sealed record AssignRoleRequest(Guid RoleId);

/// <summary>
/// Request model for sending a registration invitation.
/// </summary>
public sealed record SendInvitationRequest(
    string Email,
    string FirstNameAr,
    string LastNameAr,
    string? FirstNameEn,
    string? LastNameEn,
    Guid? RoleId,
    string? TenantName,
    string? BaseUrl);

/// <summary>
/// Request model for accepting an invitation and completing registration.
/// </summary>
public sealed record AcceptInvitationRequest(
    string Token,
    string Password,
    string ConfirmPassword,
    string? PhoneNumber);

/// <summary>
/// Request model for creating a new custom role.
/// </summary>
public sealed record CreateRoleRequest(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    List<Guid>? PermissionIds);

/// <summary>
/// Request model for updating an existing role.
/// </summary>
public sealed record UpdateRoleRequest(
    string NameAr,
    string NameEn,
    string? Description,
    List<Guid>? PermissionIds);

/// <summary>
/// Request model for activating/deactivating a role.
/// </summary>
public sealed record ToggleRoleStatusRequest(bool Activate);
