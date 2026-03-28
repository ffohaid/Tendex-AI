using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TendexAI.IntegrationTests.Infrastructure;

namespace TendexAI.IntegrationTests.Auth;

/// <summary>
/// Integration tests for the password reset flow (forgot-password and reset-password).
/// Tests cover the full flow, edge cases, and security considerations.
/// </summary>
[Collection("Integration")]
public sealed class PasswordResetIntegrationTests : IntegrationTestBase
{
    public PasswordResetIntegrationTests(TendexWebApplicationFactory factory) : base(factory) { }

    // -------------------------------------------------------------------------
    // Forgot Password Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ForgotPassword_WithValidEmail_ShouldReturn200()
    {
        // Arrange
        var request = new
        {
            Email = TestAdminEmail,
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_ShouldStillReturn200()
    {
        // Arrange - Non-existent email should still return 200 to prevent enumeration
        var request = new
        {
            Email = "nonexistent@test-tenant.gov.sa",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        // Assert - Should return 200 to prevent email enumeration
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithEmptyEmail_ShouldReturn400()
    {
        // Arrange
        var request = new
        {
            Email = "",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_WithInvalidEmailFormat_ShouldReturn400()
    {
        // Arrange
        var request = new
        {
            Email = "not-an-email",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_WithWrongTenantId_ShouldStillReturn200()
    {
        // Arrange - Wrong tenant should still return 200 to prevent enumeration
        var request = new
        {
            Email = TestAdminEmail,
            TenantId = Guid.NewGuid()
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        // Assert - Should return 200 to prevent tenant enumeration
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // Reset Password Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ResetPassword_WithInvalidSession_ShouldReturn400()
    {
        // Arrange
        var request = new
        {
            SessionId = "invalid-session-id",
            Token = "invalid-token",
            NewPassword = "NewSecureP@ss123!",
            ConfirmPassword = "NewSecureP@ss123!",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithMismatchedPasswords_ShouldReturn400()
    {
        // Arrange
        var request = new
        {
            SessionId = "some-session",
            Token = "some-token",
            NewPassword = "NewSecureP@ss123!",
            ConfirmPassword = "DifferentPassword123!",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithWeakPassword_ShouldReturn400()
    {
        // Arrange - Password without special character
        var request = new
        {
            SessionId = "some-session",
            Token = "some-token",
            NewPassword = "weakpass",
            ConfirmPassword = "weakpass",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithEmptySessionId_ShouldReturn400()
    {
        // Arrange
        var request = new
        {
            SessionId = "",
            Token = "some-token",
            NewPassword = "NewSecureP@ss123!",
            ConfirmPassword = "NewSecureP@ss123!",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithPasswordNoUppercase_ShouldReturn400()
    {
        // Arrange
        var request = new
        {
            SessionId = "some-session",
            Token = "some-token",
            NewPassword = "nouppercase123!",
            ConfirmPassword = "nouppercase123!",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithPasswordNoDigit_ShouldReturn400()
    {
        // Arrange
        var request = new
        {
            SessionId = "some-session",
            Token = "some-token",
            NewPassword = "NoDigitPassword!",
            ConfirmPassword = "NoDigitPassword!",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithPasswordNoSpecialChar_ShouldReturn400()
    {
        // Arrange
        var request = new
        {
            SessionId = "some-session",
            Token = "some-token",
            NewPassword = "NoSpecialChar123",
            ConfirmPassword = "NoSpecialChar123",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
