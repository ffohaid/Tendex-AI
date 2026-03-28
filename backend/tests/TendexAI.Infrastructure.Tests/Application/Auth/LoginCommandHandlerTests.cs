using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Features.Auth.Commands;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Infrastructure.Tests.Application.Auth;

/// <summary>
/// Unit tests for <see cref="LoginCommandHandler"/>.
/// </summary>
public sealed class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<ISessionStore> _sessionStoreMock = new();
    private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock = new();
    private readonly IConfiguration _configuration;
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:MaxFailedLoginAttempts"] = "5",
                ["Authentication:LockoutDurationMinutes"] = "15"
            })
            .Build();

        _sut = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _sessionStoreMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_User_Not_Found()
    {
        // Arrange
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var command = new LoginCommand("unknown@example.com", "password", "127.0.0.1", null, Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Account_Is_Deactivated()
    {
        // Arrange
        var user = new ApplicationUser("test@example.com", "Test", "User", null, Guid.NewGuid());
        user.Deactivate();

        _userRepositoryMock.Setup(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new LoginCommand("test@example.com", "password", "127.0.0.1", null, Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("deactivated");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Password_Is_Invalid()
    {
        // Arrange
        var user = new ApplicationUser("test@example.com", "Test", "User", null, Guid.NewGuid());
        user.SetPasswordHash("hashed_password");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(x => x.VerifyPassword("wrong_password", "hashed_password"))
            .Returns(false);

        var command = new LoginCommand("test@example.com", "wrong_password", "127.0.0.1", null, Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_Should_Return_MfaRequired_When_Mfa_Is_Enabled()
    {
        // Arrange
        var user = new ApplicationUser("test@example.com", "Test", "User", null, Guid.NewGuid());
        user.SetPasswordHash("hashed_password");
        user.EnableMfa("JBSWY3DPEHPK3PXP");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(x => x.VerifyPassword("correct_password", "hashed_password"))
            .Returns(true);

        _sessionStoreMock.Setup(x => x.CreateSessionAsync(It.IsAny<SessionData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("mfa-session-id");

        var command = new LoginCommand("test@example.com", "correct_password", "127.0.0.1", null, Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.MfaRequired.Should().BeTrue();
        result.Value.AccessToken.Should().BeEmpty();
        result.Value.SessionId.Should().Be("mfa-session-id");
    }

    [Fact]
    public async Task Handle_Should_Return_Tokens_On_Successful_Login()
    {
        // Arrange
        var user = new ApplicationUser("test@example.com", "Test", "User", null, Guid.NewGuid());
        user.SetPasswordHash("hashed_password");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync("test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(x => x.VerifyPassword("correct_password", "hashed_password"))
            .Returns(true);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        _sessionStoreMock.Setup(x => x.CreateSessionAsync(It.IsAny<SessionData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("session-id");

        var command = new LoginCommand("test@example.com", "correct_password", "127.0.0.1", null, Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.MfaRequired.Should().BeFalse();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresIn.Should().Be(3600);
        result.Value.TokenType.Should().Be("Bearer");
    }
}
