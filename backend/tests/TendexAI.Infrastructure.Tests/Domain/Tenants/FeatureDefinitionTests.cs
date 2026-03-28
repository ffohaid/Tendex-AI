using FluentAssertions;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Tests.Domain.Tenants;

/// <summary>
/// Unit tests for the FeatureDefinition entity domain logic.
/// </summary>
public sealed class FeatureDefinitionTests
{
    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Act
        var definition = new FeatureDefinition(
            featureKey: "ai_workflow_engine",
            nameAr: "محرك سير العمل الذكي",
            nameEn: "AI Workflow Engine",
            descriptionAr: "محرك ذكاء اصطناعي لأتمتة سير العمل",
            descriptionEn: "AI engine for workflow automation",
            category: "AI",
            isEnabledByDefault: true);

        // Assert
        definition.Id.Should().NotBeEmpty();
        definition.FeatureKey.Should().Be("ai_workflow_engine");
        definition.NameAr.Should().Be("محرك سير العمل الذكي");
        definition.NameEn.Should().Be("AI Workflow Engine");
        definition.Category.Should().Be("AI");
        definition.IsEnabledByDefault.Should().BeTrue();
        definition.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldModifyAllProperties()
    {
        // Arrange
        var definition = new FeatureDefinition(
            featureKey: "test",
            nameAr: "اختبار",
            nameEn: "Test",
            descriptionAr: null,
            descriptionEn: null,
            category: "General",
            isEnabledByDefault: false);

        // Act
        definition.Update(
            nameAr: "اختبار محدث",
            nameEn: "Updated Test",
            descriptionAr: "وصف عربي",
            descriptionEn: "English description",
            category: "Advanced",
            isEnabledByDefault: true);

        // Assert
        definition.NameAr.Should().Be("اختبار محدث");
        definition.NameEn.Should().Be("Updated Test");
        definition.DescriptionAr.Should().Be("وصف عربي");
        definition.DescriptionEn.Should().Be("English description");
        definition.Category.Should().Be("Advanced");
        definition.IsEnabledByDefault.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var definition = new FeatureDefinition(
            featureKey: "test",
            nameAr: "اختبار",
            nameEn: "Test",
            descriptionAr: null,
            descriptionEn: null,
            category: "General",
            isEnabledByDefault: false);

        // Act
        definition.Deactivate();

        // Assert
        definition.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Reactivate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var definition = new FeatureDefinition(
            featureKey: "test",
            nameAr: "اختبار",
            nameEn: "Test",
            descriptionAr: null,
            descriptionEn: null,
            category: "General",
            isEnabledByDefault: false);
        definition.Deactivate();

        // Act
        definition.Reactivate();

        // Assert
        definition.IsActive.Should().BeTrue();
    }
}
