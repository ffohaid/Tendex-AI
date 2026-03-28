namespace TendexAI.Application.Features.OperatorDashboard.Dtos;

/// <summary>
/// Aggregated summary for the operator (Super Admin) dashboard.
/// Contains high-level KPIs across all tenants.
/// </summary>
public sealed record OperatorDashboardSummaryDto(
    /// <summary>Total number of registered tenants (government entities).</summary>
    int TotalTenants,
    /// <summary>Number of tenants with Active status.</summary>
    int ActiveTenants,
    /// <summary>Number of tenants in Suspended status.</summary>
    int SuspendedTenants,
    /// <summary>Number of tenants pending provisioning.</summary>
    int PendingProvisioningTenants,
    /// <summary>Number of tenants in the renewal window (60 days before expiry).</summary>
    int RenewalWindowTenants,
    /// <summary>Number of tenants with Cancelled status.</summary>
    int CancelledTenants,
    /// <summary>Number of tenants with Archived status.</summary>
    int ArchivedTenants,
    /// <summary>Total active subscriptions across all tenants.</summary>
    int TotalActiveSubscriptions,
    /// <summary>Total number of feature flags configured across all tenants.</summary>
    int TotalFeatureFlags,
    /// <summary>Total number of AI configurations across all tenants.</summary>
    int TotalAiConfigurations,
    /// <summary>Total audit log entries recorded in the platform.</summary>
    long TotalAuditLogEntries,
    /// <summary>Total impersonation sessions recorded.</summary>
    int TotalImpersonationSessions,
    /// <summary>Distribution of tenants by lifecycle status.</summary>
    IReadOnlyList<TenantStatusDistributionDto> TenantStatusDistribution,
    /// <summary>Monthly tenant registration trend (last 12 months).</summary>
    IReadOnlyList<MonthlyTrendDto> MonthlyTenantRegistrations,
    /// <summary>Subscriptions expiring within the next 90 days.</summary>
    IReadOnlyList<ExpiringSubscriptionDto> ExpiringSubscriptions);

/// <summary>
/// Represents tenant count per lifecycle status for distribution charts.
/// </summary>
public sealed record TenantStatusDistributionDto(
    string StatusName,
    int StatusValue,
    int Count);

/// <summary>
/// Monthly trend data point for time-series charts.
/// </summary>
public sealed record MonthlyTrendDto(
    /// <summary>Month label in yyyy-MM format.</summary>
    string Month,
    int Count);

/// <summary>
/// Subscription approaching expiry — used for renewal alerts.
/// </summary>
public sealed record ExpiringSubscriptionDto(
    Guid TenantId,
    string TenantNameAr,
    string TenantNameEn,
    string TenantIdentifier,
    DateTime? SubscriptionExpiresAt,
    int DaysUntilExpiry);

/// <summary>
/// Per-tenant usage statistics for the cross-tenant analytics view.
/// </summary>
public sealed record TenantUsageStatisticsDto(
    Guid TenantId,
    string TenantNameAr,
    string TenantNameEn,
    string TenantIdentifier,
    string StatusName,
    int ActiveFeatureFlags,
    int TotalFeatureFlags,
    int AiConfigurationsCount,
    int AuditLogEntriesCount,
    DateTime? SubscriptionExpiresAt,
    DateTime CreatedAt);

/// <summary>
/// System health status for infrastructure monitoring.
/// </summary>
public sealed record SystemHealthStatusDto(
    string OverallStatus,
    DateTime CheckedAt,
    IReadOnlyList<ServiceHealthDto> Services);

/// <summary>
/// Individual service health check result.
/// </summary>
public sealed record ServiceHealthDto(
    string ServiceName,
    string Status,
    string? Description,
    long ResponseTimeMs);

/// <summary>
/// Resource consumption trends over time for the platform.
/// </summary>
public sealed record ResourceConsumptionTrendsDto(
    /// <summary>Daily audit log entry counts (last 30 days).</summary>
    IReadOnlyList<DailyCountDto> DailyAuditLogEntries,
    /// <summary>Daily new tenant registrations (last 30 days).</summary>
    IReadOnlyList<DailyCountDto> DailyNewTenants,
    /// <summary>Feature adoption rates across tenants.</summary>
    IReadOnlyList<FeatureAdoptionDto> FeatureAdoptionRates,
    /// <summary>AI configuration usage per provider.</summary>
    IReadOnlyList<AiProviderUsageDto> AiProviderUsage);

/// <summary>
/// Daily count data point for time-series charts.
/// </summary>
public sealed record DailyCountDto(
    /// <summary>Date in yyyy-MM-dd format.</summary>
    string Date,
    int Count);

/// <summary>
/// Feature adoption rate across tenants.
/// </summary>
public sealed record FeatureAdoptionDto(
    string FeatureKey,
    string FeatureNameAr,
    string FeatureNameEn,
    int EnabledCount,
    int TotalTenants,
    /// <summary>Adoption percentage (0-100).</summary>
    double AdoptionRate);

/// <summary>
/// AI provider usage distribution.
/// </summary>
public sealed record AiProviderUsageDto(
    string ProviderName,
    int ConfigurationsCount,
    int ActiveCount);
