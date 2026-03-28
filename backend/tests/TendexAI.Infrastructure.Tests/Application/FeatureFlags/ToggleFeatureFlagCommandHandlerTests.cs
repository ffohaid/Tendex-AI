using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Features.FeatureFlags.Commands.ToggleFeatureFlag;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Tests.Application.FeatureFlags;

/// <summary>
/// Unit tests for ToggleFeatureFlagCommandHandler.
/// </summary>
public sealed class ToggleFeatureFlagCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepoMock = new();
    private readonly Mock<ITenantFeatureFlagRepository> _featureFlagRepoMock = new();
    private readonly Mock<IFeatureDefinitionRepository> _featureDefRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<ToggleFeatureFlagCommandHandler>> _loggerMock = new();
    private readonly ToggleFeatureFlagCommandHandler _handler;

    public ToggleFeatureFlagCommandHandlerTests()
    {
        _handler = new ToggleFeatureFlagCommandHandler(
            _tenantRepoMock.Object,
            _featureFlagRepoMock.Object,
            _featureDefRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NewFlag_ShouldCreateFeatureFlag()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant(
            "وزارة المالية", "Ministry of Finance", "MOF", "mof",
            "tendex_tenant_mof", "encrypted");

        var featureDef = new FeatureDefinition(
            "ai_engine", "محرك ذكاء", "AI Engine", null, null, "AI", true);

        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _featureDefRepoMock.Setup(r => r.GetByKeyAsync("ai_engine", It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureDef);
        _featureFlagRepoMock.Setup(r => r.GetByTenantAndKeyAsync(tenantId, "ai_engine", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantFeatureFlag?)null);

        var command = new ToggleFeatureFlagCommand(tenantId, "ai_engine", true, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FeatureKey.Should().Be("ai_engine");
        result.Value.IsEnabled.Should().BeTrue();

        _featureFlagRepoMock.Verify(
            r => r.AddAsync(It.IsAny<TenantFeatureFlag>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingFlag_ShouldUpdateFeatureFlag()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant(
            "وزارة المالية", "Ministry of Finance", "MOF", "mof",
            "tendex_tenant_mof", "encrypted");

        var featureDef = new FeatureDefinition(
            "ai_engine", "محرك ذكاء", "AI Engine", null, null, "AI", true);

        var existingFlag = new TenantFeatureFlag(
            tenantId, "ai_engine", "محرك ذكاء", "AI Engine", true);

        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _featureDefRepoMock.Setup(r => r.GetByKeyAsync("ai_engine", It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureDef);
        _featureFlagRepoMock.Setup(r => r.GetByTenantAndKeyAsync(tenantId, "ai_engine", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFlag);

        var command = new ToggleFeatureFlagCommand(tenantId, "ai_engine", false, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsEnabled.Should().BeFalse();

        _featureFlagRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<TenantFeatureFlag>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TenantNotFound_ShouldReturnFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var command = new ToggleFeatureFlagCommand(tenantId, "ai_engine", true, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_FeatureDefinitionNotFound_ShouldReturnFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant(
            "وزارة المالية", "Ministry of Finance", "MOF", "mof",
            "tendex_tenant_mof", "encrypted");

        _tenantRepoMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _featureDefRepoMock.Setup(r => r.GetByKeyAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureDefinition?)null);

        var command = new ToggleFeatureFlagCommand(tenantId, "nonexistent", true, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }
}
