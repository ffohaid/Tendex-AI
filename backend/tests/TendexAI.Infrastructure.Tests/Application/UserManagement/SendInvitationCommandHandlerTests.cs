using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Features.UserManagement.Commands.SendInvitation;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Infrastructure.Tests.Application.UserManagement;

public sealed class SendInvitationCommandHandlerTests
{
    private readonly Mock<IUserInvitationRepository> _invitationRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<SendInvitationCommandHandler>> _loggerMock;
    private readonly SendInvitationCommandHandler _handler;

    public SendInvitationCommandHandlerTests()
    {
        _invitationRepoMock = new Mock<IUserInvitationRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<SendInvitationCommandHandler>>();

        _handler = new SendInvitationCommandHandler(
            _invitationRepoMock.Object,
            _userRepoMock.Object,
            _emailServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    private static SendInvitationCommand CreateValidCommand()
    {
        return new SendInvitationCommand(
            Email: "newuser@example.com",
            FirstNameAr: "محمد",
            LastNameAr: "أحمد",
            FirstNameEn: "Mohammed",
            LastNameEn: "Ahmed",
            RoleId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            InvitedByUserId: Guid.NewGuid(),
            InviterName: "Admin User",
            TenantName: "Test Tenant",
            BaseUrl: "https://app.tendex.ai");
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenUserDoesNotExist()
    {
        // Arrange
        var command = CreateValidCommand();
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _invitationRepoMock.Setup(r => r.HasPendingInvitationAsync(command.Email, command.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _emailServiceMock.Setup(e => e.SendInvitationEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _invitationRepoMock.Verify(r => r.AddAsync(It.IsAny<UserInvitation>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(e => e.SendInvitationEmailAsync(
            command.Email, It.IsAny<string>(), It.IsAny<string>(),
            command.TenantName, command.InviterName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenUserAlreadyExists()
    {
        // Arrange
        var command = CreateValidCommand();
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
        _invitationRepoMock.Verify(r => r.AddAsync(It.IsAny<UserInvitation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenPendingInvitationExists()
    {
        // Arrange
        var command = CreateValidCommand();
        _userRepoMock.Setup(r => r.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _invitationRepoMock.Setup(r => r.HasPendingInvitationAsync(command.Email, command.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("pending invitation");
    }
}
