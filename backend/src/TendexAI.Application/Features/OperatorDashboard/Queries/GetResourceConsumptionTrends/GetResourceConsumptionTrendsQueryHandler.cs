using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.OperatorDashboard.Dtos;
using TendexAI.Domain.Common;
using System.Globalization;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.OperatorDashboard.Queries.GetResourceConsumptionTrends;

/// <summary>
/// Handles retrieval of resource consumption trends for the operator dashboard.
/// Aggregates daily activity, feature adoption, and AI provider usage data.
/// </summary>
public sealed class GetResourceConsumptionTrendsQueryHandler
    : IQueryHandler<GetResourceConsumptionTrendsQuery, ResourceConsumptionTrendsDto>
{
    private readonly IMasterPlatformDbContext _context;

    public GetResourceConsumptionTrendsQueryHandler(IMasterPlatformDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ResourceConsumptionTrendsDto>> Handle(
        GetResourceConsumptionTrendsQuery request,
        CancellationToken cancellationToken)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        // --- Daily audit log entries (last 30 days) ---
        var dailyAuditLogs = await _context.GetDbSet<AuditLogEntry>()
            .Where(al => al.TimestampUtc >= thirtyDaysAgo)
            .GroupBy(al => al.TimestampUtc.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        var dailyAuditLogDtos = dailyAuditLogs
            .Select(x => new DailyCountDto(
                Date: x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Count: x.Count))
            .ToList();

        // --- Daily new tenant registrations (last 30 days) ---
        var dailyNewTenants = await _context.GetDbSet<Tenant>()
            .Where(t => t.CreatedAt >= thirtyDaysAgo)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        var dailyNewTenantDtos = dailyNewTenants
            .Select(x => new DailyCountDto(
                Date: x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Count: x.Count))
            .ToList();

        // --- Feature adoption rates ---
        var totalTenants = await _context.GetDbSet<Tenant>().CountAsync(cancellationToken);

        var featureAdoption = await _context.GetDbSet<TenantFeatureFlag>()
            .Where(ff => ff.IsEnabled)
            .GroupBy(ff => new { ff.FeatureKey, ff.FeatureNameAr, ff.FeatureNameEn })
            .Select(g => new
            {
                g.Key.FeatureKey,
                g.Key.FeatureNameAr,
                g.Key.FeatureNameEn,
                EnabledCount = g.Count()
            })
            .OrderByDescending(x => x.EnabledCount)
            .ToListAsync(cancellationToken);

        var featureAdoptionDtos = featureAdoption
            .Select(x => new FeatureAdoptionDto(
                FeatureKey: x.FeatureKey,
                FeatureNameAr: x.FeatureNameAr,
                FeatureNameEn: x.FeatureNameEn,
                EnabledCount: x.EnabledCount,
                TotalTenants: totalTenants,
                AdoptionRate: totalTenants > 0
                    ? Math.Round((double)x.EnabledCount / totalTenants * 100, 1)
                    : 0))
            .ToList();

        // --- AI provider usage distribution ---
        var aiProviderUsage = await _context.GetDbSet<AiConfiguration>()
            .GroupBy(ac => ac.Provider)
            .Select(g => new
            {
                Provider = g.Key,
                Total = g.Count(),
                Active = g.Count(ac => ac.IsActive)
            })
            .ToListAsync(cancellationToken);

        var aiProviderUsageDtos = aiProviderUsage
            .Select(x => new AiProviderUsageDto(
                ProviderName: x.Provider.ToString(),
                ConfigurationsCount: x.Total,
                ActiveCount: x.Active))
            .ToList();

        var result = new ResourceConsumptionTrendsDto(
            DailyAuditLogEntries: dailyAuditLogDtos,
            DailyNewTenants: dailyNewTenantDtos,
            FeatureAdoptionRates: featureAdoptionDtos,
            AiProviderUsage: aiProviderUsageDtos);

        return Result.Success(result);
    }
}
