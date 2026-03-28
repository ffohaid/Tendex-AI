using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Application.Common.IntegrationEvents;

/// <summary>
/// Integration event published when a new tenant (government entity)
/// is created on the platform. This event is consumed by other services
/// to provision tenant-specific resources (e.g., database, storage, vector DB).
/// </summary>
public sealed record TenantCreatedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// The unique identifier of the newly created tenant.
    /// </summary>
    public required Guid TenantEntityId { get; init; }

    /// <summary>
    /// The Arabic name of the tenant organization.
    /// </summary>
    public required string NameAr { get; init; }

    /// <summary>
    /// The English name of the tenant organization.
    /// </summary>
    public required string NameEn { get; init; }

    /// <summary>
    /// The database connection string provisioned for this tenant.
    /// </summary>
    public required string DatabaseConnectionString { get; init; }
}
