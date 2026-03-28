namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a tenant (government entity) on the platform.
/// Follows the Government PO Lifecycle as defined in the PRD:
/// PendingProvisioning -> EnvironmentSetup -> Training -> FinalAcceptance -> Active -> RenewalWindow -> Renewed/Cancelled -> Archived
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// Stage 1: PO Received. Tenant record created, awaiting automated provisioning.
    /// </summary>
    PendingProvisioning = 0,

    /// <summary>
    /// Stage 2: Environment Setup. Database provisioned, subdomain configured,
    /// Owner account created, branding applied, feature flags set.
    /// </summary>
    EnvironmentSetup = 1,

    /// <summary>
    /// Stage 3: Training. The government entity's team is being trained on the platform.
    /// </summary>
    Training = 2,

    /// <summary>
    /// Stage 4: Final Acceptance. The entity has formally accepted the system.
    /// </summary>
    FinalAcceptance = 3,

    /// <summary>
    /// Stage 5: Active Operation. The tenant is fully operational.
    /// </summary>
    Active = 4,

    /// <summary>
    /// Stage 6: Renewal Window. 60 days before subscription expiry, renewal reminders sent.
    /// </summary>
    RenewalWindow = 5,

    /// <summary>
    /// Tenant has been temporarily suspended by the platform operator.
    /// Users cannot log in, but all data is preserved.
    /// </summary>
    Suspended = 6,

    /// <summary>
    /// Tenant subscription has been cancelled (not renewed).
    /// 30-day grace period before archiving.
    /// </summary>
    Cancelled = 7,

    /// <summary>
    /// Tenant data has been archived for long-term retention.
    /// </summary>
    Archived = 8
}
