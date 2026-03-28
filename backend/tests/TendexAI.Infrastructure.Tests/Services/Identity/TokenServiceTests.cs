using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Infrastructure.Services.Identity;

namespace TendexAI.Infrastructure.Tests.Services.Identity;

/// <summary>
/// Unit tests for <see cref="TokenService"/>.
/// </summary>
public sealed class TokenServiceTests
{
    private readonly TokenService _sut;
    private const string TestSigningKey = "ThisIsAVerySecureSigningKeyForTesting1234567890!@#$";

    public TokenServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:SigningKey"] = TestSigningKey,
                ["Authentication:Issuer"] = "https://test.tendex-ai.com",
                ["Authentication:Audience"] = "test-client"
            })
            .Build();

        _sut = new TokenService(configuration);
    }

    private static ApplicationUser CreateTestUser()
    {
        return new ApplicationUser(
            "test@example.com",
            "Test",
            "User",
            "en",
            Guid.NewGuid());
    }

    [Fact]
    public void GenerateAccessToken_Should_Return_Valid_JWT()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "Admin" };
        var permissions = new List<string> { "rfp.create", "rfp.read" };
        var tenantId = Guid.NewGuid();

        // Act
        var token = _sut.GenerateAccessToken(user, roles, permissions, tenantId);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3, "JWT should have 3 parts separated by dots");
    }

    [Fact]
    public void GenerateAccessToken_Should_Contain_User_Claims()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new List<string> { "Admin" };
        var permissions = new List<string> { "rfp.create" };
        var tenantId = Guid.NewGuid();

        // Act
        var token = _sut.GenerateAccessToken(user, roles, permissions, tenantId);
        var principal = _sut.ValidateAccessToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Claims.Should().Contain(c => c.Type == "sub" && c.Value == user.Id.ToString());
        principal.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
    }

    [Fact]
    public void GenerateRefreshToken_Should_Return_NonEmpty_String()
    {
        // Act
        var refreshToken = _sut.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateRefreshToken_Should_Return_Unique_Tokens()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void ValidateAccessToken_Should_Return_Null_For_Invalid_Token()
    {
        // Act
        var result = _sut.ValidateAccessToken("invalid.token.here");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_Should_Return_UserId_For_Valid_Token()
    {
        // Arrange
        var user = CreateTestUser();
        var token = _sut.GenerateAccessToken(user, [], [], Guid.NewGuid());

        // Act
        var userId = _sut.GetUserIdFromToken(token);

        // Assert
        userId.Should().NotBeNull();
        userId.Should().Be(user.Id);
    }

    [Fact]
    public void GetUserIdFromToken_Should_Return_Null_For_Invalid_Token()
    {
        // Act
        var userId = _sut.GetUserIdFromToken("invalid.token");

        // Assert
        userId.Should().BeNull();
    }
}
