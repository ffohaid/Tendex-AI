using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Represents a refresh token issued to a user for session renewal.
/// Refresh tokens have an 8-hour validity period as per project requirements.
/// </summary>
public sealed class RefreshToken : BaseEntity<Guid>
{
    private RefreshToken() { } // EF Core parameterless constructor

    public RefreshToken(
        Guid userId,
        string token,
        DateTime expiresAt,
        string ipAddress,
        string? userAgent)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        IsRevoked = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>The user this refresh token belongs to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Navigation property to the user.</summary>
    public ApplicationUser User { get; private set; } = null!;

    /// <summary>The hashed refresh token value.</summary>
    public string Token { get; private set; } = null!;

    /// <summary>Expiration timestamp (UTC). Default: 8 hours from creation.</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Whether this token has been revoked.</summary>
    public bool IsRevoked { get; private set; }

    /// <summary>Timestamp when the token was revoked (UTC).</summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>IP address from which the token was issued.</summary>
    public string IpAddress { get; private set; } = null!;

    /// <summary>User agent string from the requesting client.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>The replacement token if this one was rotated.</summary>
    public string? ReplacedByToken { get; private set; }

    /// <summary>Reason for revocation (e.g., "Logout", "TokenRotation", "SecurityBreach").</summary>
    public string? RevocationReason { get; private set; }

    // ----- Domain Methods -----

    /// <summary>Checks if the refresh token has expired.</summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>Checks if the refresh token is still active (not expired and not revoked).</summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    /// <summary>Revokes this refresh token.</summary>
    public void Revoke(string reason, string? replacedByToken = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
        ReplacedByToken = replacedByToken;
        LastModifiedAt = DateTime.UtcNow;
    }
}
