namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a tenant (government entity) on the platform.
/// </summary>
public enum TenantStatus
{
    /// <summary>Tenant has been created but not yet fully provisioned.</summary>
    Pending = 0,

    /// <summary>Tenant is active and fully operational.</summary>
    Active = 1,

    /// <summary>Tenant has been temporarily suspended by the platform operator.</summary>
    Suspended = 2,

    /// <summary>Tenant has been permanently deactivated.</summary>
    Deactivated = 3
}
