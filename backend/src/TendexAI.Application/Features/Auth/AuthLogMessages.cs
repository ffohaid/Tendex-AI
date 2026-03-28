using Microsoft.Extensions.Logging;

namespace TendexAI.Application.Features.Auth;

/// <summary>
/// High-performance logging messages for authentication operations.
/// Uses source-generated LoggerMessage delegates (CA1848/CA1873 compliant).
/// </summary>
internal static partial class AuthLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} logged in successfully from {IpAddress}")]
    internal static partial void LogUserLoggedIn(ILogger logger, Guid userId, string ipAddress);

    [LoggerMessage(Level = LogLevel.Information, Message = "Token refreshed for user {UserId}")]
    internal static partial void LogTokenRefreshed(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} logged out")]
    internal static partial void LogUserLoggedOut(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "MFA enabled for user {UserId}")]
    internal static partial void LogMfaEnabled(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "MFA disabled for user {UserId}")]
    internal static partial void LogMfaDisabled(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "MFA verified for user {UserId}")]
    internal static partial void LogMfaVerified(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Potential token reuse detected for user {UserId}")]
    internal static partial void LogTokenReuseDetected(ILogger logger, Guid userId);
}
