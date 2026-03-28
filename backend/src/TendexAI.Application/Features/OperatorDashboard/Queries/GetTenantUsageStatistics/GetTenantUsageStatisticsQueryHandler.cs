using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.OperatorDashboard.Dtos;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.OperatorDashboard.Queries.GetTenantUsageStatistics;

/// <summary>
/// Handles retrieval of per-tenant usage statistics with pagination.
/// Aggregates feature flag, AI configuration, and audit log data per tenant.
/// </summary>
public sealed class GetTenantUsageStatisticsQueryHandler
    : IQueryHandler<GetTenantUsageStatisticsQuery, PagedResultDto<TenantUsageStatisticsDto>>
{
    private readonly IMasterPlatformDbContext _context;

    public GetTenantUsageStatisticsQueryHandler(IMasterPlatformDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResultDto<TenantUsageStatisticsDto>>> Handle(
        GetTenantUsageStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize > 100 ? 100 : request.PageSize;

        var tenantsQuery = _context.GetDbSet<Tenant>().AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            tenantsQuery = tenantsQuery.Where(t =>
                t.NameAr.ToLower().Contains(term) ||
                t.NameEn.ToLower().Contains(term) ||
                t.Identifier.ToLower().Contains(term));
        }

        var totalCount = await tenantsQuery.CountAsync(cancellationToken);

        var featureFlagsDbSet = _context.GetDbSet<TenantFeatureFlag>();
        var aiConfigsDbSet = _context.GetDbSet<AiConfiguration>();
        var auditLogsDbSet = _context.GetDbSet<AuditLogEntry>();

        // Get paginated tenants with their usage stats via left joins
        var tenantIds = await tenantsQuery
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var tenants = await tenantsQuery
            .Where(t => tenantIds.Contains(t.Id))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        // Batch load related counts
        var featureFlagCounts = await featureFlagsDbSet
            .Where(ff => tenantIds.Contains(ff.TenantId))
            .GroupBy(ff => ff.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                Total = g.Count(),
                Active = g.Count(ff => ff.IsEnabled)
            })
            .ToDictionaryAsync(x => x.TenantId, cancellationToken);

        var aiConfigCounts = await aiConfigsDbSet
            .Where(ac => tenantIds.Contains(ac.TenantId))
            .GroupBy(ac => ac.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, cancellationToken);

        var auditLogCounts = await auditLogsDbSet
            .Where(al => al.TenantId.HasValue && tenantIds.Contains(al.TenantId.Value))
            .GroupBy(al => al.TenantId!.Value)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, cancellationToken);

        var items = tenants.Select(t =>
        {
            featureFlagCounts.TryGetValue(t.Id, out var ffData);
            aiConfigCounts.TryGetValue(t.Id, out var aiData);
            auditLogCounts.TryGetValue(t.Id, out var alData);

            return new TenantUsageStatisticsDto(
                TenantId: t.Id,
                TenantNameAr: t.NameAr,
                TenantNameEn: t.NameEn,
                TenantIdentifier: t.Identifier,
                StatusName: t.Status.ToString(),
                ActiveFeatureFlags: ffData?.Active ?? 0,
                TotalFeatureFlags: ffData?.Total ?? 0,
                AiConfigurationsCount: aiData?.Count ?? 0,
                AuditLogEntriesCount: alData?.Count ?? 0,
                SubscriptionExpiresAt: t.SubscriptionExpiresAt,
                CreatedAt: t.CreatedAt);
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var result = new PagedResultDto<TenantUsageStatisticsDto>(
            Items: items,
            TotalCount: totalCount,
            PageNumber: pageNumber,
            PageSize: pageSize,
            TotalPages: totalPages);

        return Result.Success(result);
    }
}
