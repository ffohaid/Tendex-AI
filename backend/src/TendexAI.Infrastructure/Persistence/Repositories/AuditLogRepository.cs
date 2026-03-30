using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAuditLogRepository"/>.
/// Supports only append and read operations (Append-Only pattern).
/// </summary>
public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly TenantDbContext _context;

    public AuditLogRepository(TenantDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByTenantIdAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
