using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Represents a government entity (tenant) registered on the platform.
/// Each tenant is assigned an isolated database (Database-per-Tenant model).
/// </summary>
public sealed class Tenant : AggregateRoot<Guid>
{
    private Tenant() { } // EF Core parameterless constructor

    public Tenant(
        string nameAr,
        string nameEn,
        string identifier,
        string connectionString,
        string databaseName)
    {
        Id = Guid.NewGuid();
        NameAr = nameAr;
        NameEn = nameEn;
        Identifier = identifier;
        ConnectionString = connectionString;
        DatabaseName = databaseName;
        Status = TenantStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Arabic name of the government entity.</summary>
    public string NameAr { get; private set; } = null!;

    /// <summary>English name of the government entity.</summary>
    public string NameEn { get; private set; } = null!;

    /// <summary>Unique short identifier for the tenant (e.g., "MOF", "MOH").</summary>
    public string Identifier { get; private set; } = null!;

    /// <summary>Encrypted connection string for the tenant's isolated database.</summary>
    public string ConnectionString { get; private set; } = null!;

    /// <summary>Name of the tenant's isolated database.</summary>
    public string DatabaseName { get; private set; } = null!;

    /// <summary>Current lifecycle status of the tenant.</summary>
    public TenantStatus Status { get; private set; }

    /// <summary>Optional URL or path to the tenant's brand logo.</summary>
    public string? LogoUrl { get; private set; }

    /// <summary>Primary brand color hex code for dynamic branding.</summary>
    public string? PrimaryColor { get; private set; }

    /// <summary>Secondary brand color hex code for dynamic branding.</summary>
    public string? SecondaryColor { get; private set; }

    /// <summary>Subscription or license expiry date.</summary>
    public DateTime? SubscriptionExpiresAt { get; private set; }

    /// <summary>Navigation property: AI configurations for this tenant.</summary>
    public ICollection<AiConfiguration> AiConfigurations { get; private set; } = [];

    // ----- Domain Methods -----

    public void Activate()
    {
        Status = TenantStatus.Active;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = TenantStatus.Suspended;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = TenantStatus.Deactivated;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void UpdateBranding(string? logoUrl, string? primaryColor, string? secondaryColor)
    {
        LogoUrl = logoUrl;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void UpdateConnectionString(string encryptedConnectionString)
    {
        ConnectionString = encryptedConnectionString;
        LastModifiedAt = DateTime.UtcNow;
    }
}
