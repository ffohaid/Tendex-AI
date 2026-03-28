namespace TendexAI.Infrastructure.Services.Caching;

/// <summary>
/// Centralized cache key definitions for the Tendex AI platform (TASK-703).
///
/// Naming convention: {Entity}:{Scope}:{Identifier}
/// Examples:
/// - "tenant:config:550e8400-e29b-41d4-a716-446655440000"
/// - "competition:list:tenant:550e8400:page:1:size:10"
/// - "user:permissions:550e8400-e29b-41d4-a716-446655440000"
///
/// TTL guidelines:
/// - Tenant configuration: 30 minutes (rarely changes).
/// - Competition lists: 5 minutes (changes on CRUD operations).
/// - User permissions: 15 minutes (changes on role assignment).
/// - AI configuration: 60 minutes (admin-only changes).
/// - Dashboard statistics: 10 minutes (aggregated data).
/// </summary>
public static class CacheKeys
{
    // ----- Tenant Cache Keys -----

    /// <summary>Cache key for tenant configuration by tenant ID.</summary>
    public static string TenantConfig(Guid tenantId) => $"tenant:config:{tenantId}";

    /// <summary>Cache key for tenant by subdomain.</summary>
    public static string TenantBySubdomain(string subdomain) => $"tenant:subdomain:{subdomain}";

    /// <summary>Cache key for tenant by identifier.</summary>
    public static string TenantByIdentifier(string identifier) => $"tenant:identifier:{identifier}";

    /// <summary>Cache key for tenant feature flags.</summary>
    public static string TenantFeatureFlags(Guid tenantId) => $"tenant:features:{tenantId}";

    /// <summary>Cache key for tenant branding settings.</summary>
    public static string TenantBranding(Guid tenantId) => $"tenant:branding:{tenantId}";

    // ----- Competition Cache Keys -----

    /// <summary>Cache key for paginated competition list.</summary>
    public static string CompetitionList(Guid tenantId, int page, int size, string? status = null) =>
        $"competition:list:{tenantId}:p{page}:s{size}:{status ?? "all"}";

    /// <summary>Cache key for competition detail.</summary>
    public static string CompetitionDetail(Guid competitionId) => $"competition:detail:{competitionId}";

    /// <summary>Cache key for competition count by tenant.</summary>
    public static string CompetitionCount(Guid tenantId) => $"competition:count:{tenantId}";

    // ----- User Cache Keys -----

    /// <summary>Cache key for user permissions.</summary>
    public static string UserPermissions(Guid userId) => $"user:permissions:{userId}";

    /// <summary>Cache key for user roles.</summary>
    public static string UserRoles(Guid userId) => $"user:roles:{userId}";

    // ----- AI Configuration Cache Keys -----

    /// <summary>Cache key for active AI configuration.</summary>
    public static string AiConfiguration() => "ai:config:active";

    // ----- Dashboard Cache Keys -----

    /// <summary>Cache key for operator dashboard summary.</summary>
    public static string OperatorDashboardSummary() => "dashboard:operator:summary";

    /// <summary>Cache key for system health status.</summary>
    public static string SystemHealthStatus() => "dashboard:health:status";

    // ----- Prefix Patterns for Invalidation -----

    /// <summary>Prefix for all tenant-related cache keys.</summary>
    public static string TenantPrefix(Guid tenantId) => $"tenant:*:{tenantId}";

    /// <summary>Prefix for all competition-related cache keys for a tenant.</summary>
    public static string CompetitionPrefix(Guid tenantId) => $"competition:*:{tenantId}";

    /// <summary>Prefix for all user-related cache keys.</summary>
    public static string UserPrefix(Guid userId) => $"user:*:{userId}";

    // ----- TTL Constants -----

    /// <summary>TTL for tenant configuration cache (30 minutes).</summary>
    public static readonly TimeSpan TenantConfigTtl = TimeSpan.FromMinutes(30);

    /// <summary>TTL for competition list cache (5 minutes).</summary>
    public static readonly TimeSpan CompetitionListTtl = TimeSpan.FromMinutes(5);

    /// <summary>TTL for competition detail cache (10 minutes).</summary>
    public static readonly TimeSpan CompetitionDetailTtl = TimeSpan.FromMinutes(10);

    /// <summary>TTL for user permissions cache (15 minutes).</summary>
    public static readonly TimeSpan UserPermissionsTtl = TimeSpan.FromMinutes(15);

    /// <summary>TTL for AI configuration cache (60 minutes).</summary>
    public static readonly TimeSpan AiConfigTtl = TimeSpan.FromMinutes(60);

    /// <summary>TTL for dashboard statistics cache (10 minutes).</summary>
    public static readonly TimeSpan DashboardTtl = TimeSpan.FromMinutes(10);
}
