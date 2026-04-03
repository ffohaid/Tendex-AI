using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Features.UserManagement.Commands.AcceptInvitation;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Infrastructure.Tests.Application.UserManagement;

public sealed class AcceptInvitationCommandHandlerTests
{
    private readonly Mock<IUserInvitationRepository> _invitationRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<AcceptInvitationCommandHandler>> _loggerMock;
    private readonly AcceptInvitationCommandHandler _handler;

    public AcceptInvitationCommandHandlerTests()
    {
        _invitationRepoMock = new Mock<IUserInvitationRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _loggerMock = new Mock<ILogger<AcceptInvitationCommandHandler>>();

        _handler = new AcceptInvitationCommandHandler(
            _invitationRepoMock.Object,
            _userRepoMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);
    }

    private static UserInvitation CreateValidInvitation()
    {
        return new UserInvitation(
            email: "test@example.com",
            firstNameAr: "محمد",
            lastNameAr: "أحمد",
            tenantId: Guid.NewGuid(),
            invitedByUserId: Guid.NewGuid(),
            roleId: Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenInvitationIsValid()
    {
        // Arrange
        var invitation = CreateValidInvitation();
        var command = new AcceptInvitationCommand(
            Token: invitation.Token,
            Password: "StrongP@ss1",
            ConfirmPassword: "StrongP@ss1",
            PhoneNumber: "+966501234567");

        _invitationRepoMock.Setup(r => r.GetByTokenAsync(command.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(invitation.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.HashPassword(command.Password))
            .Returns("hashed_password");
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(r => r.AddUserRoleAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTokenIsInvalid()
    {
        // Arrange
        var command = new AcceptInvitationCommand("invalid_token", "StrongP@ss1", "StrongP@ss1", null);
        _invitationRepoMock.Setup(r => r.GetByTokenAsync(command.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInvitation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenUserAlreadyExists()
    {
        // Arrange
        var invitation = CreateValidInvitation();
        var command = new AcceptInvitationCommand(invitation.Token, "StrongP@ss1", "StrongP@ss1", null);

        _invitationRepoMock.Setup(r => r.GetByTokenAsync(command.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(invitation.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }
}
