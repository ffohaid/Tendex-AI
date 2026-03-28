namespace TendexAI.Application.Common.Interfaces.Identity;

/// <summary>
/// Represents session data stored in Redis.
/// </summary>
public sealed class SessionData
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = null!;
    public string IpAddress { get; set; } = null!;
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool MfaVerified { get; set; }
    public List<string> Roles { get; set; } = [];
}

/// <summary>
/// Abstraction for session storage operations.
/// </summary>
public interface ISessionStore
{
    /// <summary>Creates a new session and returns the session ID.</summary>
    Task<string> CreateSessionAsync(SessionData sessionData, CancellationToken cancellationToken = default);

    /// <summary>Retrieves session data by session ID.</summary>
    Task<SessionData?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Revokes a specific session.</summary>
    Task RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Revokes all sessions for a specific user.</summary>
    Task RevokeAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Extends the expiration of a session.</summary>
    Task ExtendSessionAsync(string sessionId, TimeSpan duration, CancellationToken cancellationToken = default);
}
