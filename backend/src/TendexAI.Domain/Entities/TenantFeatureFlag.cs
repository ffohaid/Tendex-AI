using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Represents a feature flag configuration for a specific tenant.
/// Enables fine-grained control over which features are available to each government entity.
/// Feature flags are managed by the Super Admin via the operator portal.
/// </summary>
public sealed class TenantFeatureFlag : BaseEntity<Guid>
{
    private TenantFeatureFlag() { } // EF Core parameterless constructor

    public TenantFeatureFlag(
        Guid tenantId,
        string featureKey,
        string featureNameAr,
        string featureNameEn,
        bool isEnabled,
        string? configuration = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        FeatureKey = featureKey;
        FeatureNameAr = featureNameAr;
        FeatureNameEn = featureNameEn;
        IsEnabled = isEnabled;
        Configuration = configuration;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Foreign key to the owning tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Navigation property to the owning tenant.</summary>
    public Tenant Tenant { get; private set; } = null!;

    /// <summary>
    /// Unique key identifier for the feature (e.g., "ai_workflow_engine", "advanced_analytics").
    /// Used programmatically to check feature availability.
    /// </summary>
    public string FeatureKey { get; private set; } = null!;

    /// <summary>Arabic display name of the feature.</summary>
    public string FeatureNameAr { get; private set; } = null!;

    /// <summary>English display name of the feature.</summary>
    public string FeatureNameEn { get; private set; } = null!;

    /// <summary>Whether this feature is currently enabled for the tenant.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Optional JSON configuration for the feature (e.g., limits, thresholds).
    /// Allows per-tenant customization of feature behavior.
    /// </summary>
    public string? Configuration { get; private set; }

    // ----- Domain Methods -----

    /// <summary>
    /// Enables this feature for the tenant.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Disables this feature for the tenant.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the feature flag configuration.
    /// </summary>
    public void UpdateConfiguration(string? configuration)
    {
        Configuration = configuration;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the display names of the feature.
    /// </summary>
    public void UpdateNames(string featureNameAr, string featureNameEn)
    {
        FeatureNameAr = featureNameAr;
        FeatureNameEn = featureNameEn;
        LastModifiedAt = DateTime.UtcNow;
    }
}
