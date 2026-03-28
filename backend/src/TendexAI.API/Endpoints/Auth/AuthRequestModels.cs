namespace TendexAI.API.Endpoints.Auth;

/// <summary>
/// Request model for user login.
/// </summary>
public sealed record LoginRequest(
    string Email,
    string Password,
    Guid TenantId);

/// <summary>
/// Request model for token refresh.
/// </summary>
public sealed record RefreshTokenRequest(
    string RefreshToken,
    Guid TenantId);

/// <summary>
/// Request model for logout.
/// </summary>
public sealed record LogoutRequest(
    string? RefreshToken,
    string? SessionId);

/// <summary>
/// Request model for MFA verification.
/// </summary>
public sealed record VerifyMfaRequest(
    string SessionId,
    string Code,
    Guid TenantId);

/// <summary>
/// Request model for MFA setup.
/// </summary>
public sealed record SetupMfaRequest(
    Guid TenantId);

/// <summary>
/// Request model for disabling MFA.
/// </summary>
public sealed record DisableMfaRequest(
    string Code,
    Guid TenantId);

/// <summary>
/// Request model for initiating a password reset (forgot password).
/// </summary>
public sealed record ForgotPasswordRequest(
    string Email,
    Guid TenantId);

/// <summary>
/// Request model for resetting the password with a valid token.
/// </summary>
public sealed record ResetPasswordRequest(
    string SessionId,
    string Token,
    string NewPassword,
    string ConfirmPassword,
    Guid TenantId);
