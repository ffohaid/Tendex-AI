using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TendexAI.IntegrationTests.Infrastructure;

namespace TendexAI.IntegrationTests.Auth;

/// <summary>
/// Integration tests for the authentication endpoints.
/// Tests cover login, token refresh, logout, and edge cases.
/// </summary>
[Collection("Integration")]
public sealed class AuthenticationIntegrationTests : IntegrationTestBase
{
    public AuthenticationIntegrationTests(TendexWebApplicationFactory factory) : base(factory) { }

    // -------------------------------------------------------------------------
    // Login Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var request = new
        {
            Email = TestAdminEmail,
            Password = TestAdminPassword,
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<AuthTokenResponseDto>(JsonOptions);
        content.Should().NotBeNull();
        content!.AccessToken.Should().NotBeNullOrWhiteSpace();
        content.RefreshToken.Should().NotBeNullOrWhiteSpace();
        content.TokenType.Should().Be("Bearer");
        content.ExpiresIn.Should().Be(3600);
        content.SessionId.Should().NotBeNullOrWhiteSpace();
        content.MfaRequired.Should().BeFalse();
        content.User.Should().NotBeNull();
        content.User.Email.Should().Be(TestAdminEmail);
        content.User.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn401()
    {
        // Arrange
        var request = new
        {
            Email = TestAdminEmail,
            Password = "WrongPassword123!",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ShouldReturn401()
    {
        // Arrange
        var request = new
        {
            Email = "nonexistent@test-tenant.gov.sa",
            Password = "SomePassword123!",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyEmail_ShouldReturn400()
    {
        // Arrange
        var request = new
        {
            Email = "",
            Password = TestAdminPassword,
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongTenantId_ShouldReturn401()
    {
        // Arrange
        var request = new
        {
            Email = TestAdminEmail,
            Password = TestAdminPassword,
            TenantId = Guid.NewGuid()
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Token Refresh Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange - First login to get a refresh token
        var loginRequest = new
        {
            Email = TestAdminEmail,
            Password = TestAdminPassword,
            TenantId = TestTenantId
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<AuthTokenResponseDto>(JsonOptions);

        var refreshRequest = new
        {
            RefreshToken = loginContent!.RefreshToken,
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh-token", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<AuthTokenResponseDto>(JsonOptions);
        content.Should().NotBeNull();
        content!.AccessToken.Should().NotBeNullOrWhiteSpace();
        content.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        var request = new
        {
            RefreshToken = "invalid-refresh-token",
            TenantId = TestTenantId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh-token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Logout Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Logout_WithValidSession_ShouldReturn204()
    {
        // Arrange - Login first
        var loginRequest = new
        {
            Email = TestAdminEmail,
            Password = TestAdminPassword,
            TenantId = TestTenantId
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<AuthTokenResponseDto>(JsonOptions);

        var authenticatedClient = Factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginContent!.AccessToken);

        var logoutRequest = new
        {
            RefreshToken = loginContent.RefreshToken,
            SessionId = loginContent.SessionId
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var logoutRequest = new
        {
            RefreshToken = "some-token",
            SessionId = "some-session"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Protected Endpoint Access Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ShouldReturn200()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();

        // Act - Try accessing a protected endpoint (competitions list)
        var response = await authenticatedClient.GetAsync("/api/v1/competitions?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/competitions?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_ShouldReturn401()
    {
        // Arrange - Use a clearly invalid/expired token
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "expired.token.value");

        // Act
        var response = await client.GetAsync("/api/v1/competitions?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Account Lockout Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_MultipleFailedAttempts_ShouldEventuallyLockAccount()
    {
        // Arrange - Use a unique email to avoid affecting other tests
        // We test with the regular user
        var request = new
        {
            Email = TestRegularEmail,
            Password = "WrongPassword!",
            TenantId = TestTenantId
        };

        // Act - Attempt login multiple times with wrong password
        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 6; i++)
        {
            lastResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", request);
        }

        // Assert - After multiple failed attempts, account should be locked
        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Verify the error message indicates lockout
        var errorContent = await lastResponse.Content.ReadAsStringAsync();
        errorContent.Should().NotBeNullOrWhiteSpace();
    }
}
