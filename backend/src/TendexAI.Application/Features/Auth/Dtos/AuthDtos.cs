namespace TendexAI.Application.Features.Auth.Dtos;

/// <summary>
/// Response DTO returned after successful authentication.
/// </summary>
public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    string SessionId,
    UserInfoDto User,
    bool MfaRequired);

/// <summary>
/// Basic user information included in auth responses.
/// </summary>
public sealed record UserInfoDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    Guid TenantId,
    bool MfaEnabled,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

/// <summary>
/// Response DTO for MFA setup operations.
/// </summary>
public sealed record MfaSetupResponse(
    string SecretKey,
    string QrCodeUri,
    IReadOnlyList<string> RecoveryCodes);

/// <summary>
/// Response DTO for MFA verification.
/// </summary>
public sealed record MfaVerifyResponse(
    bool Success,
    AuthTokenResponse? TokenResponse);
