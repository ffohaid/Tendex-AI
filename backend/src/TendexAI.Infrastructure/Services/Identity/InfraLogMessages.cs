using Microsoft.Extensions.Logging;

namespace TendexAI.Infrastructure.Services.Identity;

/// <summary>
/// High-performance logging messages for infrastructure identity services.
/// Uses source-generated LoggerMessage delegates (CA1848/CA1873 compliant).
/// </summary>
internal static partial class InfraLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Session created for user {UserId}: {SessionId}")]
    internal static partial void LogSessionCreated(ILogger logger, Guid userId, string sessionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Session revoked: {SessionId}")]
    internal static partial void LogSessionRevoked(ILogger logger, string sessionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "All sessions revoked for user {UserId}. Count: {Count}")]
    internal static partial void LogAllSessionsRevoked(ILogger logger, Guid userId, int count);
}
