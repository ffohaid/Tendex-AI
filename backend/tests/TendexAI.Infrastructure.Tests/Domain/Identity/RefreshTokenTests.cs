using FluentAssertions;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Infrastructure.Tests.Domain.Identity;

/// <summary>
/// Unit tests for <see cref="RefreshToken"/> domain entity.
/// </summary>
public sealed class RefreshTokenTests
{
    [Fact]
    public void Constructor_Should_Initialize_Properties_Correctly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "test-refresh-token";
        var expiresAt = DateTime.UtcNow.AddHours(8);
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        // Act
        var refreshToken = new RefreshToken(userId, token, expiresAt, ipAddress, userAgent);

        // Assert
        refreshToken.UserId.Should().Be(userId);
        refreshToken.Token.Should().Be(token);
        refreshToken.ExpiresAt.Should().Be(expiresAt);
        refreshToken.IpAddress.Should().Be(ipAddress);
        refreshToken.UserAgent.Should().Be(userAgent);
        refreshToken.IsRevoked.Should().BeFalse();
        refreshToken.IsExpired.Should().BeFalse();
        refreshToken.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Should_Return_True_When_Token_Is_Past_Expiry()
    {
        // Arrange
        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            "test-token",
            DateTime.UtcNow.AddHours(-1),
            "192.168.1.1",
            null);

        // Assert
        refreshToken.IsExpired.Should().BeTrue();
        refreshToken.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_Should_Set_Revoked_Properties()
    {
        // Arrange
        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            "test-token",
            DateTime.UtcNow.AddHours(8),
            "192.168.1.1",
            null);

        // Act
        refreshToken.Revoke("Logout");

        // Assert
        refreshToken.IsRevoked.Should().BeTrue();
        refreshToken.RevokedAt.Should().NotBeNull();
        refreshToken.RevocationReason.Should().Be("Logout");
        refreshToken.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_With_Replacement_Should_Set_ReplacedByToken()
    {
        // Arrange
        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            "old-token",
            DateTime.UtcNow.AddHours(8),
            "192.168.1.1",
            null);

        // Act
        refreshToken.Revoke("TokenRotation", "new-token");

        // Assert
        refreshToken.IsRevoked.Should().BeTrue();
        refreshToken.ReplacedByToken.Should().Be("new-token");
        refreshToken.RevocationReason.Should().Be("TokenRotation");
    }
}
