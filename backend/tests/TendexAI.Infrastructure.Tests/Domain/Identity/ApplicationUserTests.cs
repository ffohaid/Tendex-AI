using FluentAssertions;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Infrastructure.Tests.Domain.Identity;

/// <summary>
/// Unit tests for <see cref="ApplicationUser"/> domain entity.
/// </summary>
public sealed class ApplicationUserTests
{
    private static ApplicationUser CreateTestUser()
    {
        return new ApplicationUser(
            "test@example.com",
            "Ahmed",
            "Ali",
            null,
            Guid.NewGuid());
    }

    [Fact]
    public void Constructor_Should_Initialize_Properties_Correctly()
    {
        // Arrange & Act
        var tenantId = Guid.NewGuid();
        var user = new ApplicationUser("test@example.com", "Ahmed", "Ali", "0500000000", tenantId);

        // Assert
        user.Email.Should().Be("test@example.com");
        user.FirstName.Should().Be("Ahmed");
        user.LastName.Should().Be("Ali");
        user.PhoneNumber.Should().Be("0500000000");
        user.TenantId.Should().Be(tenantId);
        user.IsActive.Should().BeTrue();
        user.MfaEnabled.Should().BeFalse();
        user.AccessFailedCount.Should().Be(0);
        user.SecurityStamp.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SetPasswordHash_Should_Update_PasswordHash_And_SecurityStamp()
    {
        // Arrange
        var user = CreateTestUser();
        var originalStamp = user.SecurityStamp;
        var hash = "$2a$12$somehashedpassword";

        // Act
        user.SetPasswordHash(hash);

        // Assert
        user.PasswordHash.Should().Be(hash);
        user.SecurityStamp.Should().NotBe(originalStamp);
    }

    [Fact]
    public void RecordFailedLogin_Should_Increment_AccessFailedCount()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.RecordFailedLogin(5, TimeSpan.FromMinutes(15));

        // Assert
        user.AccessFailedCount.Should().Be(1);
    }

    [Fact]
    public void RecordFailedLogin_Should_Lockout_After_Max_Attempts()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        for (var i = 0; i < 5; i++)
        {
            user.RecordFailedLogin(5, TimeSpan.FromMinutes(15));
        }

        // Assert
        user.AccessFailedCount.Should().Be(5);
        user.LockoutEnd.Should().NotBeNull();
        user.IsLockedOut().Should().BeTrue();
    }

    [Fact]
    public void RecordSuccessfulLogin_Should_Reset_Failed_Attempts()
    {
        // Arrange
        var user = CreateTestUser();
        user.RecordFailedLogin(5, TimeSpan.FromMinutes(15));
        user.RecordFailedLogin(5, TimeSpan.FromMinutes(15));

        // Act
        user.RecordSuccessfulLogin("192.168.1.1");

        // Assert
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginIp.Should().Be("192.168.1.1");
    }

    [Fact]
    public void EnableMfa_Should_Set_MfaEnabled_And_SecretKey()
    {
        // Arrange
        var user = CreateTestUser();
        var secretKey = "JBSWY3DPEHPK3PXP";

        // Act
        user.EnableMfa(secretKey);

        // Assert
        user.MfaEnabled.Should().BeTrue();
        user.MfaSecretKey.Should().Be(secretKey);
    }

    [Fact]
    public void DisableMfa_Should_Clear_MfaSettings()
    {
        // Arrange
        var user = CreateTestUser();
        user.EnableMfa("JBSWY3DPEHPK3PXP");

        // Act
        user.DisableMfa();

        // Assert
        user.MfaEnabled.Should().BeFalse();
        user.MfaSecretKey.Should().BeNull();
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_False()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_Should_Set_IsActive_True()
    {
        // Arrange
        var user = CreateTestUser();
        user.Deactivate();

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsLockedOut_Should_Return_False_When_LockoutEnd_Is_Past()
    {
        // Arrange
        var user = CreateTestUser();
        for (var i = 0; i < 5; i++)
        {
            user.RecordFailedLogin(5, TimeSpan.FromMilliseconds(1));
        }

        // Wait for lockout to expire
        Thread.Sleep(10);

        // Act & Assert
        user.IsLockedOut().Should().BeFalse();
    }
}
