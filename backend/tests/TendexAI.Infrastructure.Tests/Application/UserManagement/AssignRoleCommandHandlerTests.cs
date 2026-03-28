using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Features.UserManagement.Commands.AssignRole;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Infrastructure.Tests.Application.UserManagement;

public sealed class AssignRoleCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IRoleRepository> _roleRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<AssignRoleCommandHandler>> _loggerMock;
    private readonly AssignRoleCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public AssignRoleCommandHandlerTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _roleRepoMock = new Mock<IRoleRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<AssignRoleCommandHandler>>();

        _handler = new AssignRoleCommandHandler(
            _userRepoMock.Object,
            _roleRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenUserAndRoleExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = new ApplicationUser("test@example.com", "Test", "User", null, _tenantId);
        var role = new Role("مشرف", "Admin", "ADMIN", _tenantId, true);

        // Use reflection to set IDs since they are protected
        typeof(ApplicationUser).GetProperty("Id")!.SetValue(user, userId);
        typeof(Role).GetProperty("Id")!.SetValue(role, roleId);

        var command = new AssignRoleCommand(userId, roleId, _tenantId, "admin-user");

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _roleRepoMock.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _userRepoMock.Setup(r => r.HasRoleAsync(userId, roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userRepoMock.Verify(r => r.AddUserRoleAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        var command = new AssignRoleCommand(Guid.NewGuid(), Guid.NewGuid(), _tenantId, "admin");
        _userRepoMock.Setup(r => r.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenUserAlreadyHasRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = new ApplicationUser("test@example.com", "Test", "User", null, _tenantId);
        var role = new Role("مشرف", "Admin", "ADMIN", _tenantId, true);

        typeof(ApplicationUser).GetProperty("Id")!.SetValue(user, userId);
        typeof(Role).GetProperty("Id")!.SetValue(role, roleId);

        var command = new AssignRoleCommand(userId, roleId, _tenantId, "admin");

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _roleRepoMock.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _userRepoMock.Setup(r => r.HasRoleAsync(userId, roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already has this role");
    }
}
