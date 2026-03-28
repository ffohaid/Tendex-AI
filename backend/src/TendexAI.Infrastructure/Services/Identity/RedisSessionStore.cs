using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.Identity;

namespace TendexAI.Infrastructure.Services.Identity;

/// <summary>
/// Manages user session data in Redis for distributed session storage.
/// Supports session creation, validation, and revocation.
/// </summary>
public sealed class RedisSessionStore : ISessionStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisSessionStore> _logger;

    private const string SessionPrefix = "session:";
    private const string UserSessionsPrefix = "user_sessions:";

    public RedisSessionStore(IDistributedCache cache, ILogger<RedisSessionStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> CreateSessionAsync(SessionData sessionData, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var sessionKey = $"{SessionPrefix}{sessionId}";

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8) // Match refresh token validity
        };

        var serialized = JsonSerializer.Serialize(sessionData);
        await _cache.SetStringAsync(sessionKey, serialized, options, cancellationToken);

        // Track session under user's session set
        var userSessionsKey = $"{UserSessionsPrefix}{sessionData.UserId}";
        var existingSessions = await GetUserSessionIdsAsync(sessionData.UserId, cancellationToken);
        existingSessions.Add(sessionId);
        await _cache.SetStringAsync(
            userSessionsKey,
            JsonSerializer.Serialize(existingSessions),
            options,
            cancellationToken);

        InfraLogMessages.LogSessionCreated(_logger, sessionData.UserId, sessionId);
        return sessionId;
    }

    /// <inheritdoc />
    public async Task<SessionData?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var sessionKey = $"{SessionPrefix}{sessionId}";
        var serialized = await _cache.GetStringAsync(sessionKey, cancellationToken);

        if (serialized is null)
            return null;

        return JsonSerializer.Deserialize<SessionData>(serialized);
    }

    /// <inheritdoc />
    public async Task RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var sessionKey = $"{SessionPrefix}{sessionId}";
        var session = await GetSessionAsync(sessionId, cancellationToken);

        await _cache.RemoveAsync(sessionKey, cancellationToken);

        if (session is not null)
        {
            var userSessionsKey = $"{UserSessionsPrefix}{session.UserId}";
            var existingSessions = await GetUserSessionIdsAsync(session.UserId, cancellationToken);
            existingSessions.Remove(sessionId);

            if (existingSessions.Count > 0)
            {
                await _cache.SetStringAsync(
                    userSessionsKey,
                    JsonSerializer.Serialize(existingSessions),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8) },
                    cancellationToken);
            }
            else
            {
                await _cache.RemoveAsync(userSessionsKey, cancellationToken);
            }
        }

        InfraLogMessages.LogSessionRevoked(_logger, sessionId);
    }

    /// <inheritdoc />
    public async Task RevokeAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sessionIds = await GetUserSessionIdsAsync(userId, cancellationToken);

        foreach (var sessionId in sessionIds)
        {
            var sessionKey = $"{SessionPrefix}{sessionId}";
            await _cache.RemoveAsync(sessionKey, cancellationToken);
        }

        var userSessionsKey = $"{UserSessionsPrefix}{userId}";
        await _cache.RemoveAsync(userSessionsKey, cancellationToken);

        InfraLogMessages.LogAllSessionsRevoked(_logger, userId, sessionIds.Count);
    }

    /// <inheritdoc />
    public async Task ExtendSessionAsync(string sessionId, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session is null) return;

        var sessionKey = $"{SessionPrefix}{sessionId}";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duration
        };

        var serialized = JsonSerializer.Serialize(session);
        await _cache.SetStringAsync(sessionKey, serialized, options, cancellationToken);
    }

    private async Task<List<string>> GetUserSessionIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userSessionsKey = $"{UserSessionsPrefix}{userId}";
        var serialized = await _cache.GetStringAsync(userSessionsKey, cancellationToken);

        if (serialized is null)
            return [];

        return JsonSerializer.Deserialize<List<string>>(serialized) ?? [];
    }
}
