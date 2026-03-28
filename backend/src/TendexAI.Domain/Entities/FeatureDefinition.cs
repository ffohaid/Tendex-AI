using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Represents a global feature definition available on the platform.
/// These definitions serve as a catalog of all features that can be toggled per tenant.
/// Managed by the Super Admin at the platform level.
/// </summary>
public sealed class FeatureDefinition : BaseEntity<Guid>
{
    private FeatureDefinition() { } // EF Core parameterless constructor

    public FeatureDefinition(
        string featureKey,
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string category,
        bool isEnabledByDefault)
    {
        Id = Guid.NewGuid();
        FeatureKey = featureKey;
        NameAr = nameAr;
        NameEn = nameEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        Category = category;
        IsEnabledByDefault = isEnabledByDefault;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unique key identifier for the feature (e.g., "ai_workflow_engine").
    /// Must match the FeatureKey in TenantFeatureFlag.
    /// </summary>
    public string FeatureKey { get; private set; } = null!;

    /// <summary>Arabic display name.</summary>
    public string NameAr { get; private set; } = null!;

    /// <summary>English display name.</summary>
    public string NameEn { get; private set; } = null!;

    /// <summary>Arabic description of what this feature does.</summary>
    public string? DescriptionAr { get; private set; }

    /// <summary>English description of what this feature does.</summary>
    public string? DescriptionEn { get; private set; }

    /// <summary>
    /// Category grouping for the feature (e.g., "AI", "Workflow", "Analytics", "Security").
    /// </summary>
    public string Category { get; private set; } = null!;

    /// <summary>Whether this feature is enabled by default for new tenants.</summary>
    public bool IsEnabledByDefault { get; private set; }

    /// <summary>Whether this feature definition is still active in the catalog.</summary>
    public bool IsActive { get; private set; }

    // ----- Domain Methods -----

    /// <summary>
    /// Updates the feature definition details.
    /// </summary>
    public void Update(
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string category,
        bool isEnabledByDefault)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        Category = category;
        IsEnabledByDefault = isEnabledByDefault;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates this feature definition (soft delete).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates a previously deactivated feature definition.
    /// </summary>
    public void Reactivate()
    {
        IsActive = true;
        LastModifiedAt = DateTime.UtcNow;
    }
}
