namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Provides the current tenant context for multi-tenancy resolution.
/// Implementation resides in the Infrastructure layer.
/// </summary>
public interface ITenantProvider
{
    /// <summary>Gets the current tenant identifier from the request context.</summary>
    Guid? GetCurrentTenantId();

    /// <summary>Gets the connection string for the current tenant's isolated database.</summary>
    string? GetCurrentTenantConnectionString();
}
