using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.OperatorDashboard.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.OperatorDashboard.Queries.GetOperatorDashboardSummary;

/// <summary>
/// Handles the aggregation of operator dashboard summary data.
/// Queries the master platform database for cross-tenant KPIs and analytics.
/// </summary>
public sealed class GetOperatorDashboardSummaryQueryHandler
    : IQueryHandler<GetOperatorDashboardSummaryQuery, OperatorDashboardSummaryDto>
{
    private readonly IMasterPlatformDbContext _context;

    public GetOperatorDashboardSummaryQueryHandler(IMasterPlatformDbContext context)
    {
        _context = context;
    }

    public async Task<Result<OperatorDashboardSummaryDto>> Handle(
        GetOperatorDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var tenantsDbSet = _context.GetDbSet<Tenant>();
        var featureFlagsDbSet = _context.GetDbSet<TenantFeatureFlag>();
        var aiConfigsDbSet = _context.GetDbSet<AiConfiguration>();
        var auditLogsDbSet = _context.GetDbSet<AuditLogEntry>();
        var impersonationDbSet = _context.GetDbSet<ImpersonationSession>();

        // --- Tenant counts by status ---
        var tenantStatusCounts = await tenantsDbSet
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalTenants = tenantStatusCounts.Sum(x => x.Count);
        var activeTenants = tenantStatusCounts
            .FirstOrDefault(x => x.Status == TenantStatus.Active)?.Count ?? 0;
        var suspendedTenants = tenantStatusCounts
            .FirstOrDefault(x => x.Status == TenantStatus.Suspended)?.Count ?? 0;
        var pendingProvisioningTenants = tenantStatusCounts
            .FirstOrDefault(x => x.Status == TenantStatus.PendingProvisioning)?.Count ?? 0;
        var renewalWindowTenants = tenantStatusCounts
            .FirstOrDefault(x => x.Status == TenantStatus.RenewalWindow)?.Count ?? 0;
        var cancelledTenants = tenantStatusCounts
            .FirstOrDefault(x => x.Status == TenantStatus.Cancelled)?.Count ?? 0;
        var archivedTenants = tenantStatusCounts
            .FirstOrDefault(x => x.Status == TenantStatus.Archived)?.Count ?? 0;

        // --- Status distribution for charts ---
        var statusDistribution = tenantStatusCounts
            .Select(x => new TenantStatusDistributionDto(
                StatusName: x.Status.ToString(),
                StatusValue: (int)x.Status,
                Count: x.Count))
            .OrderBy(x => x.StatusValue)
            .ToList();

        // --- Active subscription count (tenants with future expiry) ---
        var totalActiveSubscriptions = await tenantsDbSet
            .CountAsync(t => t.SubscriptionExpiresAt != null && t.SubscriptionExpiresAt > DateTime.UtcNow, cancellationToken);

        // --- Feature flags count ---
        var totalFeatureFlags = await featureFlagsDbSet
            .CountAsync(cancellationToken);

        // --- AI configurations count ---
        var totalAiConfigurations = await aiConfigsDbSet
            .CountAsync(cancellationToken);

        // --- Audit log entries count ---
        var totalAuditLogEntries = await auditLogsDbSet
            .LongCountAsync(cancellationToken);

        // --- Impersonation sessions count ---
        var totalImpersonationSessions = await impersonationDbSet
            .CountAsync(cancellationToken);

        // --- Monthly tenant registrations (last 12 months) ---
        var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
        var monthlyRegistrations = await tenantsDbSet
            .Where(t => t.CreatedAt >= twelveMonthsAgo)
            .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        var monthlyTrends = monthlyRegistrations
            .Select(x => new MonthlyTrendDto(
                Month: $"{x.Year:D4}-{x.Month:D2}",
                Count: x.Count))
            .ToList();

        // --- Expiring subscriptions (next 90 days) ---
        var now = DateTime.UtcNow;
        var ninetyDaysFromNow = now.AddDays(90);
        var expiringTenants = await tenantsDbSet
            .Where(t => t.SubscriptionExpiresAt != null
                        && t.SubscriptionExpiresAt > now
                        && t.SubscriptionExpiresAt <= ninetyDaysFromNow
                        && t.Status != TenantStatus.Cancelled
                        && t.Status != TenantStatus.Archived)
            .OrderBy(t => t.SubscriptionExpiresAt)
            .Select(t => new ExpiringSubscriptionDto(
                TenantId: t.Id,
                TenantNameAr: t.NameAr,
                TenantNameEn: t.NameEn,
                TenantIdentifier: t.Identifier,
                SubscriptionExpiresAt: t.SubscriptionExpiresAt,
                DaysUntilExpiry: t.SubscriptionExpiresAt.HasValue
                    ? (int)(t.SubscriptionExpiresAt.Value - now).TotalDays
                    : 0))
            .ToListAsync(cancellationToken);

        var summary = new OperatorDashboardSummaryDto(
            TotalTenants: totalTenants,
            ActiveTenants: activeTenants,
            SuspendedTenants: suspendedTenants,
            PendingProvisioningTenants: pendingProvisioningTenants,
            RenewalWindowTenants: renewalWindowTenants,
            CancelledTenants: cancelledTenants,
            ArchivedTenants: archivedTenants,
            TotalActiveSubscriptions: totalActiveSubscriptions,
            TotalFeatureFlags: totalFeatureFlags,
            TotalAiConfigurations: totalAiConfigurations,
            TotalAuditLogEntries: totalAuditLogEntries,
            TotalImpersonationSessions: totalImpersonationSessions,
            TenantStatusDistribution: statusDistribution,
            MonthlyTenantRegistrations: monthlyTrends,
            ExpiringSubscriptions: expiringTenants);

        return Result.Success(summary);
    }
}
