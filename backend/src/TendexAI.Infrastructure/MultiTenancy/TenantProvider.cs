using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.MultiTenancy;

/// <summary>
/// Resolves the current tenant from the HTTP request context.
/// Tenant identification is performed via the "X-Tenant-Id" header.
/// The resolved tenant's connection string is cached per-request to avoid repeated DB lookups.
/// Connection strings may be stored encrypted (AES-256) or as plain text;
/// this provider handles both cases gracefully.
/// </summary>
public sealed class TenantProvider : ITenantProvider
{
    private const string TenantHeaderName = "X-Tenant-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MasterPlatformDbContext _masterDb;
    private readonly IConnectionStringEncryptor _encryptor;
    private readonly ILogger<TenantProvider> _logger;

    private Guid? _cachedTenantId;
    private string? _cachedConnectionString;
    private bool _isResolved;

    public TenantProvider(
        IHttpContextAccessor httpContextAccessor,
        MasterPlatformDbContext masterDb,
        IConnectionStringEncryptor encryptor,
        ILogger<TenantProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _masterDb = masterDb;
        _encryptor = encryptor;
        _logger = logger;
    }

    public Guid? GetCurrentTenantId()
    {
        EnsureResolved();
        return _cachedTenantId;
    }

    public string? GetCurrentTenantConnectionString()
    {
        EnsureResolved();
        return _cachedConnectionString;
    }

    private void EnsureResolved()
    {
        if (_isResolved)
        {
            return;
        }

        _isResolved = true;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        var tenantHeader = httpContext.Request.Headers[TenantHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantHeader) || !Guid.TryParse(tenantHeader, out var tenantId))
        {
            return;
        }

        // Look up the tenant in the master database (synchronous to avoid deadlocks in DI resolution)
        var tenant = _masterDb.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId && t.Status == Domain.Enums.TenantStatus.Active)
            .Select(t => new { t.Id, t.ConnectionString })
            .FirstOrDefault();

        if (tenant is null)
        {
            return;
        }

        _cachedTenantId = tenant.Id;

        // Resolve connection string - handle both encrypted and plain text formats
        if (!string.IsNullOrWhiteSpace(tenant.ConnectionString))
        {
            _cachedConnectionString = ResolveConnectionString(tenant.ConnectionString, tenantId);
        }
    }

    /// <summary>
    /// Attempts to decrypt the connection string. If decryption fails (e.g., the string
    /// is already in plain text format), falls back to using the raw value.
    /// This handles the case where the platform-operator tenant has an unencrypted
    /// connection string while government tenants have encrypted ones.
    /// </summary>
    private string ResolveConnectionString(string storedValue, Guid tenantId)
    {
        // Quick check: if it looks like a plain SQL Server connection string, use it directly
        if (storedValue.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
            storedValue.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            return storedValue;
        }

        // Otherwise, try to decrypt it (AES-256 encrypted)
        try
        {
            return _encryptor.Decrypt(storedValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to decrypt connection string for tenant {TenantId}. " +
                "Attempting to use as plain text.", tenantId);
            return storedValue;
        }
    }
}
