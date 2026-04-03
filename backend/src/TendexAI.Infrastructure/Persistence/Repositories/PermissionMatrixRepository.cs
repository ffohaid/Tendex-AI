using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for the flexible permission matrix rules.
/// </summary>
public sealed class PermissionMatrixRepository : IPermissionMatrixRepository
{
    private readonly TenantDbContext _context;

    public PermissionMatrixRepository(TenantDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionMatrixRule>> GetRulesAsync(
        Guid roleId,
        ResourceScope scope,
        ResourceType resourceType,
        CommitteeRole? committeeRole = null,
        CompetitionPhase? competitionPhase = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PermissionMatrixRules
            .AsNoTracking()
            .Where(r => r.RoleId == roleId
                && r.Scope == scope
                && r.ResourceType == resourceType
                && r.IsActive);

        if (committeeRole.HasValue)
        {
            query = query.Where(r => r.CommitteeRole == committeeRole.Value || r.CommitteeRole == null);
        }
        else
        {
            query = query.Where(r => r.CommitteeRole == null);
        }

        if (competitionPhase.HasValue)
        {
            query = query.Where(r => r.CompetitionPhase == competitionPhase.Value || r.CompetitionPhase == null);
        }
        else
        {
            query = query.Where(r => r.CompetitionPhase == null);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionMatrixRule>> GetAllRulesForRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PermissionMatrixRules
            .AsNoTracking()
            .Where(r => r.RoleId == roleId && r.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionMatrixRule>> GetAllByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PermissionMatrixRules
            .AsNoTracking()
            .Include(r => r.Role)
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .OrderBy(r => r.RoleId)
            .ThenBy(r => r.Scope)
            .ThenBy(r => r.ResourceType)
            .ThenBy(r => r.CompetitionPhase)
            .ThenBy(r => r.CommitteeRole)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PermissionMatrixRule?> GetRuleAsync(
        Guid roleId,
        ResourceScope scope,
        ResourceType resourceType,
        CommitteeRole? committeeRole = null,
        CompetitionPhase? competitionPhase = null,
        CancellationToken cancellationToken = default)
    {
        return await _context.PermissionMatrixRules
            .FirstOrDefaultAsync(r =>
                r.RoleId == roleId
                && r.Scope == scope
                && r.ResourceType == resourceType
                && r.CommitteeRole == committeeRole
                && r.CompetitionPhase == competitionPhase
                && r.IsActive,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PermissionMatrixRule?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.PermissionMatrixRules
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(
        PermissionMatrixRule rule,
        CancellationToken cancellationToken = default)
    {
        await _context.PermissionMatrixRules.AddAsync(rule, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(
        IEnumerable<PermissionMatrixRule> rules,
        CancellationToken cancellationToken = default)
    {
        await _context.PermissionMatrixRules.AddRangeAsync(rules, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAllByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var rules = await _context.PermissionMatrixRules
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        _context.PermissionMatrixRules.RemoveRange(rules);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
