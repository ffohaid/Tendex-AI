using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Infrastructure.MultiTenancy;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Application.Features.Impersonation.Queries;

/// <summary>
/// Handles searching users across all tenants for impersonation purposes.
/// This query iterates over tenant databases to find matching users.
/// Only accessible by Super Admin / Support Admin roles.
/// </summary>
public sealed class SearchUsersQueryHandler
    : IQueryHandler<SearchUsersQuery, PaginatedResponse<UserSearchResultDto>>
{
    private readonly IMasterPlatformDbContext _masterDbContext;
    private readonly IDbContextFactory<TenantDbContext> _tenantDbFactory;
    private readonly ILogger<SearchUsersQueryHandler> _logger;

    public SearchUsersQueryHandler(
        IMasterPlatformDbContext masterDbContext,
        IDbContextFactory<TenantDbContext> tenantDbFactory,
        ILogger<SearchUsersQueryHandler> logger)
    {
        _masterDbContext = masterDbContext;
        _tenantDbFactory = tenantDbFactory;
        _logger = logger;
    }

    public async Task<Result<PaginatedResponse<UserSearchResultDto>>> Handle(
        SearchUsersQuery request,
        CancellationToken cancellationToken)
    {
        var results = new List<UserSearchResultDto>();

        // 1. Get all active tenants (or filter by specific tenant)
        var tenantsQuery = _masterDbContext.GetDbSet<Tenant>()
            .AsNoTracking()
            .AsQueryable();

        if (request.TenantId.HasValue)
            tenantsQuery = tenantsQuery.Where(t => t.Id == request.TenantId.Value);

        var tenants = await tenantsQuery
            .Where(t => t.IsProvisioned)
            .Select(t => new { t.Id, t.NameAr, t.NameEn, t.ConnectionString })
            .ToListAsync(cancellationToken);

        // 2. Search users in each tenant database
        foreach (var tenant in tenants)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
                optionsBuilder.UseSqlServer(tenant.ConnectionString, sqlOptions =>
                {
                    sqlOptions.CommandTimeout(10);
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 1, maxRetryDelay: TimeSpan.FromSeconds(2), errorNumbersToAdd: null);
                });

                await using var tenantDb = new TenantDbContext(optionsBuilder.Options);

                var usersQuery = tenantDb.Users.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var term = request.SearchTerm.ToUpperInvariant();
                    usersQuery = usersQuery.Where(u =>
                        u.NormalizedEmail.Contains(term) ||
                        u.FirstName.ToUpper().Contains(term) ||
                        u.LastName.ToUpper().Contains(term));
                }

                var users = await usersQuery
                    .Select(u => new UserSearchResultDto(
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.TenantId,
                        tenant.NameAr ?? tenant.NameEn,
                        u.IsActive,
                        u.LastLoginAt))
                    .Take(50) // Limit per tenant
                    .ToListAsync(cancellationToken);

                results.AddRange(users);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to search users in tenant {TenantId}. Skipping.",
                    tenant.Id);
            }
        }

        // 3. Apply pagination on combined results
        var totalCount = results.Count;
        var pagedResults = results
            .OrderBy(u => u.Email)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Result.Success(new PaginatedResponse<UserSearchResultDto>(
            Items: pagedResults,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize));
    }
}
