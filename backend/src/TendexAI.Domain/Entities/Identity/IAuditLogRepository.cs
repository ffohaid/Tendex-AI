namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Repository interface for <see cref="AuditLog"/> entity.
/// Only supports append operations (Append-Only pattern).
/// </summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByTenantIdAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
