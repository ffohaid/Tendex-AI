using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Represents a subscription/license record for a tenant on the platform.
/// Stored in the master_platform database.
/// </summary>
public sealed class Subscription : BaseEntity<Guid>
{
    private Subscription() { } // EF Core parameterless constructor

    public Subscription(
        Guid tenantId,
        string planName,
        DateTime startsAt,
        DateTime expiresAt,
        int maxUsers)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        PlanName = planName;
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
        MaxUsers = maxUsers;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Foreign key to the owning tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Navigation property to the owning tenant.</summary>
    public Tenant Tenant { get; private set; } = null!;

    /// <summary>Name of the subscription plan.</summary>
    public string PlanName { get; private set; } = null!;

    /// <summary>Subscription start date.</summary>
    public DateTime StartsAt { get; private set; }

    /// <summary>Subscription expiry date.</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Maximum number of users allowed under this subscription.</summary>
    public int MaxUsers { get; private set; }

    /// <summary>Whether this subscription is currently active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Navigation property: the tenant that owns this subscription.</summary>

    // ----- Domain Methods -----

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void Renew(DateTime newExpiresAt)
    {
        ExpiresAt = newExpiresAt;
        IsActive = true;
        LastModifiedAt = DateTime.UtcNow;
    }
}
