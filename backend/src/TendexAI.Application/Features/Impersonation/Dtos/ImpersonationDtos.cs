namespace TendexAI.Application.Features.Impersonation.Dtos;

/// <summary>
/// Response DTO for an impersonation session.
/// </summary>
public sealed record ImpersonationSessionDto(
    Guid Id,
    Guid AdminUserId,
    string AdminUserName,
    string AdminEmail,
    Guid TargetUserId,
    string TargetUserName,
    string TargetEmail,
    Guid TargetTenantId,
    string TargetTenantName,
    string Reason,
    string? TicketReference,
    string? ConsentReference,
    string IpAddress,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status);

/// <summary>
/// Response DTO for an impersonation consent request.
/// </summary>
public sealed record ImpersonationConsentDto(
    Guid Id,
    Guid RequestedByUserId,
    string RequestedByUserName,
    Guid TargetUserId,
    string TargetUserName,
    string TargetEmail,
    Guid TargetTenantId,
    string Reason,
    string? TicketReference,
    DateTime RequestedAtUtc,
    Guid? ApprovedByUserId,
    string? ApprovedByUserName,
    DateTime? ResolvedAtUtc,
    string Status,
    string? RejectionReason,
    DateTime? ExpiresAtUtc);

/// <summary>
/// Response DTO returned when impersonation starts successfully.
/// Contains the impersonated user's access token.
/// </summary>
public sealed record ImpersonationStartResponse(
    string AccessToken,
    string SessionId,
    Guid ImpersonationSessionId,
    UserImpersonationInfo TargetUser);

/// <summary>
/// Basic info about the impersonated user.
/// </summary>
public sealed record UserImpersonationInfo(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    Guid TenantId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

/// <summary>
/// DTO for user search results (used when searching for a user to impersonate).
/// </summary>
public sealed record UserSearchResultDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    Guid TenantId,
    string? TenantName,
    bool IsActive,
    DateTime? LastLoginAt);

/// <summary>
/// Paginated response wrapper.
/// </summary>
public sealed record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    long TotalCount,
    int Page,
    int PageSize);
