using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Stores Active Directory / LDAP integration configuration per tenant.
/// This is an optional feature — each tenant can choose to enable or disable AD integration.
/// Bind passwords are encrypted with AES-256 before persistence.
/// </summary>
public sealed class ActiveDirectoryConfiguration : BaseEntity<Guid>
{
    private ActiveDirectoryConfiguration() { } // EF Core parameterless constructor

    public ActiveDirectoryConfiguration(
        Guid tenantId,
        string serverUrl,
        int port,
        string baseDn,
        string? bindDn,
        string? encryptedBindPassword,
        string? searchFilter,
        bool useSsl,
        bool useTls,
        string? userAttributeMapping,
        string? groupAttributeMapping,
        string? description)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        ServerUrl = serverUrl;
        Port = port;
        BaseDn = baseDn;
        BindDn = bindDn;
        EncryptedBindPassword = encryptedBindPassword;
        SearchFilter = searchFilter;
        UseSsl = useSsl;
        UseTls = useTls;
        UserAttributeMapping = userAttributeMapping;
        GroupAttributeMapping = groupAttributeMapping;
        Description = description;
        IsEnabled = false; // Disabled by default until tested
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Foreign key to the owning tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Navigation property to the owning tenant.</summary>
    public Tenant Tenant { get; private set; } = null!;

    /// <summary>AD/LDAP server URL (e.g., "ldap://ad.example.gov.sa" or "ldaps://ad.example.gov.sa").</summary>
    public string ServerUrl { get; private set; } = null!;

    /// <summary>AD/LDAP server port (default: 389 for LDAP, 636 for LDAPS).</summary>
    public int Port { get; private set; }

    /// <summary>Base Distinguished Name for user search (e.g., "DC=example,DC=gov,DC=sa").</summary>
    public string BaseDn { get; private set; } = null!;

    /// <summary>Bind DN for authenticating with the AD server (e.g., "CN=ServiceAccount,OU=Services,DC=example,DC=gov,DC=sa").</summary>
    public string? BindDn { get; private set; }

    /// <summary>AES-256 encrypted bind password. Decrypted in-memory only during execution.</summary>
    public string? EncryptedBindPassword { get; private set; }

    /// <summary>LDAP search filter for finding users (e.g., "(sAMAccountName={0})").</summary>
    public string? SearchFilter { get; private set; }

    /// <summary>Whether to use SSL (LDAPS) for the connection.</summary>
    public bool UseSsl { get; private set; }

    /// <summary>Whether to use StartTLS for the connection.</summary>
    public bool UseTls { get; private set; }

    /// <summary>
    /// JSON mapping of AD attributes to platform user fields.
    /// Example: {"email":"mail","firstName":"givenName","lastName":"sn","phone":"telephoneNumber"}
    /// </summary>
    public string? UserAttributeMapping { get; private set; }

    /// <summary>
    /// JSON mapping of AD groups to platform roles.
    /// Example: {"CN=Admins,OU=Groups,DC=example":"TenantPrimaryAdmin","CN=Users,OU=Groups,DC=example":"TenantUser"}
    /// </summary>
    public string? GroupAttributeMapping { get; private set; }

    /// <summary>Optional description or notes about this AD configuration.</summary>
    public string? Description { get; private set; }

    /// <summary>Whether this AD integration is currently enabled for the tenant.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Timestamp of the last successful connection test.</summary>
    public DateTime? LastConnectionTestAt { get; private set; }

    /// <summary>Result of the last connection test (true = success, false = failure).</summary>
    public bool? LastConnectionTestResult { get; private set; }

    /// <summary>Error message from the last failed connection test.</summary>
    public string? LastConnectionTestError { get; private set; }

    // ----- Domain Methods -----

    /// <summary>Enables AD integration for this tenant.</summary>
    public void Enable()
    {
        IsEnabled = true;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Disables AD integration for this tenant.</summary>
    public void Disable()
    {
        IsEnabled = false;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Updates the AD server connection settings.</summary>
    public void UpdateConnectionSettings(
        string serverUrl,
        int port,
        string baseDn,
        string? bindDn,
        string? encryptedBindPassword,
        string? searchFilter,
        bool useSsl,
        bool useTls)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDn);

        ServerUrl = serverUrl;
        Port = port;
        BaseDn = baseDn;
        BindDn = bindDn;
        if (encryptedBindPassword is not null)
            EncryptedBindPassword = encryptedBindPassword;
        SearchFilter = searchFilter;
        UseSsl = useSsl;
        UseTls = useTls;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Updates the attribute and group mapping configuration.</summary>
    public void UpdateMappings(string? userAttributeMapping, string? groupAttributeMapping)
    {
        UserAttributeMapping = userAttributeMapping;
        GroupAttributeMapping = groupAttributeMapping;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Updates the description.</summary>
    public void UpdateDescription(string? description)
    {
        Description = description;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Records the result of a connection test.</summary>
    public void RecordConnectionTest(bool success, string? errorMessage = null)
    {
        LastConnectionTestAt = DateTime.UtcNow;
        LastConnectionTestResult = success;
        LastConnectionTestError = success ? null : errorMessage;
        LastModifiedAt = DateTime.UtcNow;
    }
}
