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
/// Connection strings are stored encrypted (AES-256) and decrypted in-memory only.
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

        // Decrypt the connection string in-memory (stored encrypted with AES-256)
        if (!string.IsNullOrWhiteSpace(tenant.ConnectionString))
        {
            try
            {
                _cachedConnectionString = _encryptor.Decrypt(tenant.ConnectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt connection string for tenant {TenantId}", tenantId);
                _cachedConnectionString = null;
            }
        }
    }
}
