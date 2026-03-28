using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Represents a government entity (tenant) registered on the platform.
/// Each tenant is assigned an isolated database (Database-per-Tenant model).
/// This is the aggregate root for the Tenant bounded context.
/// </summary>
public sealed class Tenant : AggregateRoot<Guid>
{
    private readonly List<Subscription> _subscriptions = [];
    private readonly List<TenantFeatureFlag> _featureFlags = [];
    private readonly List<AiConfiguration> _aiConfigurations = [];

    private Tenant() { } // EF Core parameterless constructor

    public Tenant(
        string nameAr,
        string nameEn,
        string identifier,
        string subdomain,
        string databaseName,
        string encryptedConnectionString)
    {
        Id = Guid.NewGuid();
        NameAr = nameAr;
        NameEn = nameEn;
        Identifier = identifier;
        Subdomain = subdomain;
        DatabaseName = databaseName;
        ConnectionString = encryptedConnectionString;
        Status = TenantStatus.PendingProvisioning;
        IsProvisioned = false;
        CreatedAt = DateTime.UtcNow;
    }

    // ----- Identity Properties -----

    /// <summary>Arabic name of the government entity.</summary>
    public string NameAr { get; private set; } = null!;

    /// <summary>English name of the government entity.</summary>
    public string NameEn { get; private set; } = null!;

    /// <summary>Unique short identifier for the tenant (e.g., "MOF", "MOH").</summary>
    public string Identifier { get; private set; } = null!;

    /// <summary>Subdomain assigned to this tenant (e.g., "mof" for mof.tendex.ai).</summary>
    public string Subdomain { get; private set; } = null!;

    // ----- Database Isolation Properties -----

    /// <summary>Encrypted connection string for the tenant's isolated database.</summary>
    public string ConnectionString { get; private set; } = null!;

    /// <summary>Name of the tenant's isolated database.</summary>
    public string DatabaseName { get; private set; } = null!;

    /// <summary>Whether the tenant's database has been provisioned and migrated.</summary>
    public bool IsProvisioned { get; private set; }

    /// <summary>Timestamp when provisioning was completed.</summary>
    public DateTime? ProvisionedAt { get; private set; }

    // ----- Lifecycle Properties -----

    /// <summary>Current lifecycle status of the tenant.</summary>
    public TenantStatus Status { get; private set; }

    /// <summary>Subscription or license expiry date.</summary>
    public DateTime? SubscriptionExpiresAt { get; private set; }

    // ----- Branding Properties -----

    /// <summary>Optional URL or path to the tenant's brand logo.</summary>
    public string? LogoUrl { get; private set; }

    /// <summary>Primary brand color hex code for dynamic branding.</summary>
    public string? PrimaryColor { get; private set; }

    /// <summary>Secondary brand color hex code for dynamic branding.</summary>
    public string? SecondaryColor { get; private set; }

    // ----- Contact Properties -----

    /// <summary>Name of the primary contact person at the government entity.</summary>
    public string? ContactPersonName { get; private set; }

    /// <summary>Email of the primary contact person.</summary>
    public string? ContactPersonEmail { get; private set; }

    /// <summary>Phone number of the primary contact person.</summary>
    public string? ContactPersonPhone { get; private set; }

    // ----- Notes -----

    /// <summary>Optional notes or remarks about the tenant.</summary>
    public string? Notes { get; private set; }

    // ----- Navigation Properties -----

    /// <summary>Navigation property: subscriptions for this tenant.</summary>
    public IReadOnlyCollection<Subscription> Subscriptions => _subscriptions.AsReadOnly();

    /// <summary>Navigation property: feature flags for this tenant.</summary>
    public IReadOnlyCollection<TenantFeatureFlag> FeatureFlags => _featureFlags.AsReadOnly();

    /// <summary>Navigation property: AI configurations for this tenant.</summary>
    public IReadOnlyCollection<AiConfiguration> AiConfigurations => _aiConfigurations.AsReadOnly();

    // ----- Domain Methods: Lifecycle Management -----

    /// <summary>
    /// Marks the tenant as provisioned after database creation and initial setup.
    /// Transitions status from PendingProvisioning to EnvironmentSetup.
    /// </summary>
    public void MarkAsProvisioned()
    {
        if (Status != TenantStatus.PendingProvisioning)
            throw new InvalidOperationException(
                $"Cannot mark tenant as provisioned when status is '{Status}'. Expected '{TenantStatus.PendingProvisioning}'.");

        IsProvisioned = true;
        ProvisionedAt = DateTime.UtcNow;
        Status = TenantStatus.EnvironmentSetup;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the tenant to Training status.
    /// </summary>
    public void MoveToTraining()
    {
        if (Status != TenantStatus.EnvironmentSetup)
            throw new InvalidOperationException(
                $"Cannot move to training when status is '{Status}'. Expected '{TenantStatus.EnvironmentSetup}'.");

        Status = TenantStatus.Training;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the tenant to FinalAcceptance status.
    /// </summary>
    public void MoveToFinalAcceptance()
    {
        if (Status != TenantStatus.Training)
            throw new InvalidOperationException(
                $"Cannot move to final acceptance when status is '{Status}'. Expected '{TenantStatus.Training}'.");

        Status = TenantStatus.FinalAcceptance;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the tenant for full operation.
    /// </summary>
    public void Activate()
    {
        if (Status != TenantStatus.FinalAcceptance && Status != TenantStatus.Suspended)
            throw new InvalidOperationException(
                $"Cannot activate tenant when status is '{Status}'. Expected '{TenantStatus.FinalAcceptance}' or '{TenantStatus.Suspended}'.");

        Status = TenantStatus.Active;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Suspends the tenant. Users cannot log in but data is preserved.
    /// </summary>
    public void Suspend()
    {
        if (Status != TenantStatus.Active && Status != TenantStatus.RenewalWindow)
            throw new InvalidOperationException(
                $"Cannot suspend tenant when status is '{Status}'.");

        Status = TenantStatus.Suspended;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Moves the tenant into the renewal window period (60 days before expiry).
    /// </summary>
    public void EnterRenewalWindow()
    {
        if (Status != TenantStatus.Active)
            throw new InvalidOperationException(
                $"Cannot enter renewal window when status is '{Status}'. Expected '{TenantStatus.Active}'.");

        Status = TenantStatus.RenewalWindow;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the tenant subscription.
    /// </summary>
    public void Cancel()
    {
        if (Status == TenantStatus.Cancelled || Status == TenantStatus.Archived)
            throw new InvalidOperationException(
                $"Cannot cancel tenant when status is '{Status}'.");

        Status = TenantStatus.Cancelled;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Archives the tenant data for long-term retention.
    /// </summary>
    public void Archive()
    {
        if (Status != TenantStatus.Cancelled && Status != TenantStatus.Suspended)
            throw new InvalidOperationException(
                $"Cannot archive tenant when status is '{Status}'. Expected '{TenantStatus.Cancelled}' or '{TenantStatus.Suspended}'.");

        Status = TenantStatus.Archived;
        LastModifiedAt = DateTime.UtcNow;
    }

    // ----- Domain Methods: Branding -----

    /// <summary>
    /// Updates the visual branding for this tenant.
    /// </summary>
    public void UpdateBranding(string? logoUrl, string? primaryColor, string? secondaryColor)
    {
        LogoUrl = logoUrl;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        LastModifiedAt = DateTime.UtcNow;
    }

    // ----- Domain Methods: Connection -----

    /// <summary>
    /// Updates the encrypted connection string for the tenant's database.
    /// </summary>
    public void UpdateConnectionString(string encryptedConnectionString)
    {
        ConnectionString = encryptedConnectionString;
        LastModifiedAt = DateTime.UtcNow;
    }

    // ----- Domain Methods: Contact -----

    /// <summary>
    /// Updates the primary contact person details for this tenant.
    /// </summary>
    public void UpdateContactPerson(string? name, string? email, string? phone)
    {
        ContactPersonName = name;
        ContactPersonEmail = email;
        ContactPersonPhone = phone;
        LastModifiedAt = DateTime.UtcNow;
    }

    // ----- Domain Methods: Subscription -----

    /// <summary>
    /// Updates the subscription expiry date.
    /// </summary>
    public void UpdateSubscriptionExpiry(DateTime? expiresAt)
    {
        SubscriptionExpiresAt = expiresAt;
        LastModifiedAt = DateTime.UtcNow;
    }

    // ----- Domain Methods: Info Update -----

    /// <summary>
    /// Updates the basic information of the tenant.
    /// </summary>
    public void UpdateInfo(string nameAr, string nameEn, string? notes)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        Notes = notes;
        LastModifiedAt = DateTime.UtcNow;
    }
}
