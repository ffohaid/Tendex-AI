using FluentAssertions;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Tests.Domain.Tenants;

/// <summary>
/// Unit tests for the TenantFeatureFlag entity domain logic.
/// </summary>
public sealed class TenantFeatureFlagTests
{
    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var flag = new TenantFeatureFlag(
            tenantId: tenantId,
            featureKey: "ai_workflow_engine",
            featureNameAr: "محرك سير العمل الذكي",
            featureNameEn: "AI Workflow Engine",
            isEnabled: true,
            configuration: "{\"maxConcurrent\": 5}");

        // Assert
        flag.Id.Should().NotBeEmpty();
        flag.TenantId.Should().Be(tenantId);
        flag.FeatureKey.Should().Be("ai_workflow_engine");
        flag.FeatureNameAr.Should().Be("محرك سير العمل الذكي");
        flag.FeatureNameEn.Should().Be("AI Workflow Engine");
        flag.IsEnabled.Should().BeTrue();
        flag.Configuration.Should().Be("{\"maxConcurrent\": 5}");
    }

    [Fact]
    public void Enable_ShouldSetIsEnabledToTrue()
    {
        // Arrange
        var flag = new TenantFeatureFlag(
            tenantId: Guid.NewGuid(),
            featureKey: "test_feature",
            featureNameAr: "ميزة اختبار",
            featureNameEn: "Test Feature",
            isEnabled: false);

        // Act
        flag.Enable();

        // Assert
        flag.IsEnabled.Should().BeTrue();
        flag.LastModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void Disable_ShouldSetIsEnabledToFalse()
    {
        // Arrange
        var flag = new TenantFeatureFlag(
            tenantId: Guid.NewGuid(),
            featureKey: "test_feature",
            featureNameAr: "ميزة اختبار",
            featureNameEn: "Test Feature",
            isEnabled: true);

        // Act
        flag.Disable();

        // Assert
        flag.IsEnabled.Should().BeFalse();
        flag.LastModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateConfiguration_ShouldUpdateConfigValue()
    {
        // Arrange
        var flag = new TenantFeatureFlag(
            tenantId: Guid.NewGuid(),
            featureKey: "test_feature",
            featureNameAr: "ميزة اختبار",
            featureNameEn: "Test Feature",
            isEnabled: true);

        // Act
        flag.UpdateConfiguration("{\"limit\": 100}");

        // Assert
        flag.Configuration.Should().Be("{\"limit\": 100}");
        flag.LastModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateNames_ShouldUpdateDisplayNames()
    {
        // Arrange
        var flag = new TenantFeatureFlag(
            tenantId: Guid.NewGuid(),
            featureKey: "test_feature",
            featureNameAr: "ميزة اختبار",
            featureNameEn: "Test Feature",
            isEnabled: true);

        // Act
        flag.UpdateNames("ميزة محدثة", "Updated Feature");

        // Assert
        flag.FeatureNameAr.Should().Be("ميزة محدثة");
        flag.FeatureNameEn.Should().Be("Updated Feature");
    }
}
