using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Features.Tenants.Commands.CreateTenant;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Application.Tenants;

/// <summary>
/// Unit tests for CreateTenantCommandHandler.
/// </summary>
public sealed class CreateTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepoMock = new();
    private readonly Mock<IFeatureDefinitionRepository> _featureDefRepoMock = new();
    private readonly Mock<ITenantFeatureFlagRepository> _featureFlagRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IConnectionStringEncryptor> _encryptorMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<CreateTenantCommandHandler>> _loggerMock = new();
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests()
    {
        _encryptorMock.Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns("encrypted_value");

        _featureDefRepoMock.Setup(r => r.GetDefaultEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeatureDefinition>());

        _featureDefRepoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeatureDefinition>());

        _handler = new CreateTenantCommandHandler(
            _tenantRepoMock.Object,
            _featureDefRepoMock.Object,
            _featureFlagRepoMock.Object,
            _unitOfWorkMock.Object,
            _encryptorMock.Object,
            _configMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateTenantSuccessfully()
    {
        // Arrange
        var command = new CreateTenantCommand(
            NameAr: "وزارة المالية",
            NameEn: "Ministry of Finance",
            Identifier: "MOF",
            Subdomain: "mof",
            ContactPersonName: "Ahmed",
            ContactPersonEmail: "ahmed@mof.gov.sa",
            ContactPersonPhone: "+966501234567",
            Notes: "Test tenant",
            LogoUrl: null,
            PrimaryColor: null,
            SecondaryColor: null);

        _tenantRepoMock.Setup(r => r.ExistsByIdentifierAsync("MOF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _tenantRepoMock.Setup(r => r.ExistsBySubdomainAsync("mof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.NameAr.Should().Be("وزارة المالية");
        result.Value.NameEn.Should().Be("Ministry of Finance");
        result.Value.Identifier.Should().Be("MOF");
        result.Value.Status.Should().Be(TenantStatus.PendingProvisioning);

        _tenantRepoMock.Verify(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateIdentifier_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateTenantCommand(
            NameAr: "وزارة المالية",
            NameEn: "Ministry of Finance",
            Identifier: "MOF",
            Subdomain: "mof",
            ContactPersonName: null,
            ContactPersonEmail: null,
            ContactPersonPhone: null,
            Notes: null,
            LogoUrl: null,
            PrimaryColor: null,
            SecondaryColor: null);

        _tenantRepoMock.Setup(r => r.ExistsByIdentifierAsync("MOF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_DuplicateSubdomain_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateTenantCommand(
            NameAr: "وزارة المالية",
            NameEn: "Ministry of Finance",
            Identifier: "MOF",
            Subdomain: "mof",
            ContactPersonName: null,
            ContactPersonEmail: null,
            ContactPersonPhone: null,
            Notes: null,
            LogoUrl: null,
            PrimaryColor: null,
            SecondaryColor: null);

        _tenantRepoMock.Setup(r => r.ExistsByIdentifierAsync("MOF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _tenantRepoMock.Setup(r => r.ExistsBySubdomainAsync("mof", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_WithFeatureDefinitions_ShouldInitializeFeatureFlags()
    {
        // Arrange
        var command = new CreateTenantCommand(
            NameAr: "وزارة الصحة",
            NameEn: "Ministry of Health",
            Identifier: "MOH",
            Subdomain: "moh",
            ContactPersonName: null,
            ContactPersonEmail: null,
            ContactPersonPhone: null,
            Notes: null,
            LogoUrl: null,
            PrimaryColor: null,
            SecondaryColor: null);

        _tenantRepoMock.Setup(r => r.ExistsByIdentifierAsync("MOH", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _tenantRepoMock.Setup(r => r.ExistsBySubdomainAsync("moh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var features = new List<FeatureDefinition>
        {
            new("ai_engine", "محرك ذكاء", "AI Engine", null, null, "AI", true),
            new("analytics", "تحليلات", "Analytics", null, null, "Analytics", false)
        };

        _featureDefRepoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(features);
        _featureDefRepoMock.Setup(r => r.GetDefaultEnabledAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(features.Where(f => f.IsEnabledByDefault).ToList());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _featureFlagRepoMock.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<TenantFeatureFlag>>(flags => flags.Count() == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
