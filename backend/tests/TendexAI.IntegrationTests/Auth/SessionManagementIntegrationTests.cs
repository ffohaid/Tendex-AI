using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TendexAI.IntegrationTests.Infrastructure;

namespace TendexAI.IntegrationTests.Auth;

/// <summary>
/// Integration tests for session management.
/// Tests cover session creation, validation, revocation, and concurrent sessions.
/// </summary>
[Collection("Integration")]
public sealed class SessionManagementIntegrationTests : IntegrationTestBase
{
    public SessionManagementIntegrationTests(TendexWebApplicationFactory factory) : base(factory) { }

    // -------------------------------------------------------------------------
    // Session Creation Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_ShouldCreateSession_WithValidSessionId()
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
        content!.SessionId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MultipleLogins_ShouldCreateDifferentSessions()
    {
        // Arrange
        var request = new
        {
            Email = TestAdminEmail,
            Password = TestAdminPassword,
            TenantId = TestTenantId
        };

        // Act
        var response1 = await Client.PostAsJsonAsync("/api/v1/auth/login", request);
        var response2 = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var content1 = await response1.Content.ReadFromJsonAsync<AuthTokenResponseDto>(JsonOptions);
        var content2 = await response2.Content.ReadFromJsonAsync<AuthTokenResponseDto>(JsonOptions);

        content1!.SessionId.Should().NotBe(content2!.SessionId);
        content1.AccessToken.Should().NotBe(content2.AccessToken);
    }

    // -------------------------------------------------------------------------
    // Session Revocation Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Logout_ShouldRevokeSession_PreventingFurtherAccess()
    {
        // Arrange - Login to get tokens
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
            new AuthenticationHeaderValue("Bearer", loginContent!.AccessToken);

        // Act - Logout
        var logoutRequest = new
        {
            RefreshToken = loginContent.RefreshToken,
            SessionId = loginContent.SessionId
        };

        var logoutResponse = await authenticatedClient.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert - Refresh token should no longer work
        var refreshRequest = new
        {
            RefreshToken = loginContent.RefreshToken,
            TenantId = TestTenantId
        };

        var refreshResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh-token", refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Token Validation Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AccessToken_ShouldContainCorrectUserInfo()
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
        content!.User.Should().NotBeNull();
        content.User.Id.Should().Be(TestAdminUserId);
        content.User.Email.Should().Be(TestAdminEmail);
        content.User.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public async Task Login_ShouldReturnUserRoles()
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
        content!.User.Roles.Should().NotBeEmpty();
        content.User.Roles.Should().Contain("System Administrator");
    }

    // -------------------------------------------------------------------------
    // Concurrent Session Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ConcurrentSessions_ShouldAllBeValid()
    {
        // Arrange
        var request = new
        {
            Email = TestAdminEmail,
            Password = TestAdminPassword,
            TenantId = TestTenantId
        };

        // Act - Create multiple sessions
        var response1 = await Client.PostAsJsonAsync("/api/v1/auth/login", request);
        var response2 = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        var content1 = await response1.Content.ReadFromJsonAsync<AuthTokenResponseDto>(JsonOptions);
        var content2 = await response2.Content.ReadFromJsonAsync<AuthTokenResponseDto>(JsonOptions);

        // Assert - Both sessions should be valid
        var client1 = Factory.CreateClient();
        client1.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());
        client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", content1!.AccessToken);

        var client2 = Factory.CreateClient();
        client2.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", content2!.AccessToken);

        var accessResponse1 = await client1.GetAsync("/api/v1/competitions?page=1&pageSize=10");
        var accessResponse2 = await client2.GetAsync("/api/v1/competitions?page=1&pageSize=10");

        accessResponse1.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        accessResponse2.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
