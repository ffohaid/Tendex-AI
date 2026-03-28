using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Entities;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.MultiTenancy;

/// <summary>
/// Resolves the current tenant from the HTTP request context.
/// Tenant identification is performed via the "X-Tenant-Id" header.
/// The resolved tenant's connection string is cached per-request to avoid repeated DB lookups.
/// </summary>
public sealed class TenantProvider : ITenantProvider
{
    private const string TenantHeaderName = "X-Tenant-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MasterPlatformDbContext _masterDb;

    private Guid? _cachedTenantId;
    private string? _cachedConnectionString;
    private bool _isResolved;

    public TenantProvider(
        IHttpContextAccessor httpContextAccessor,
        MasterPlatformDbContext masterDb)
    {
        _httpContextAccessor = httpContextAccessor;
        _masterDb = masterDb;
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
        _cachedConnectionString = tenant.ConnectionString;
    }
}
