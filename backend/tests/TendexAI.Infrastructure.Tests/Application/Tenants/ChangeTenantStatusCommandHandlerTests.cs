using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Features.Tenants.Commands.ChangeTenantStatus;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Application.Tenants;

/// <summary>
/// Unit tests for ChangeTenantStatusCommandHandler.
/// </summary>
public sealed class ChangeTenantStatusCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<ChangeTenantStatusCommandHandler>> _loggerMock = new();
    private readonly ChangeTenantStatusCommandHandler _handler;

    public ChangeTenantStatusCommandHandlerTests()
    {
        _handler = new ChangeTenantStatusCommandHandler(
            _tenantRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidTransition_ShouldSucceed()
    {
        // Arrange
        var tenant = new Tenant(
            "وزارة المالية", "Ministry of Finance", "MOF", "mof",
            "tendex_tenant_mof", "encrypted");

        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var command = new ChangeTenantStatusCommand(tenant.Id, TenantStatus.EnvironmentSetup);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(TenantStatus.EnvironmentSetup);
    }

    [Fact]
    public async Task Handle_TenantNotFound_ShouldReturnFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var command = new ChangeTenantStatusCommand(tenantId, TenantStatus.Active);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_InvalidTransition_ShouldReturnFailure()
    {
        // Arrange
        var tenant = new Tenant(
            "وزارة المالية", "Ministry of Finance", "MOF", "mof",
            "tendex_tenant_mof", "encrypted");

        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Trying to activate directly from PendingProvisioning (invalid)
        var command = new ChangeTenantStatusCommand(tenant.Id, TenantStatus.Active);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot");
    }
}
