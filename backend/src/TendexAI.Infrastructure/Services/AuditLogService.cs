using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IAuditLogService"/> that persists audit entries
/// to the master platform database using an Append-Only pattern.
/// This service only performs INSERT operations; UPDATE and DELETE are strictly prohibited.
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    private readonly MasterPlatformDbContext _dbContext;

    public AuditLogService(MasterPlatformDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task LogAsync(
        Guid userId,
        string userName,
        string? ipAddress,
        AuditActionType actionType,
        string entityType,
        string entityId,
        string? oldValues,
        string? newValues,
        string? reason,
        string? sessionId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry(
            userId: userId,
            userName: userName,
            ipAddress: ipAddress,
            actionType: actionType,
            entityType: entityType,
            entityId: entityId,
            oldValues: oldValues,
            newValues: newValues,
            reason: reason,
            sessionId: sessionId,
            tenantId: tenantId);

        _dbContext.AuditLogEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLogEntry>> GetLogsAsync(
        Guid? tenantId = null,
        Guid? userId = null,
        AuditActionType? actionType = null,
        string? entityType = null,
        string? entityId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilteredQuery(tenantId, userId, actionType, entityType, entityId, fromUtc, toUtc);

        return await query
            .OrderByDescending(e => e.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> GetLogsCountAsync(
        Guid? tenantId = null,
        Guid? userId = null,
        AuditActionType? actionType = null,
        string? entityType = null,
        string? entityId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilteredQuery(tenantId, userId, actionType, entityType, entityId, fromUtc, toUtc);
        return await query.LongCountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// Builds a filtered IQueryable based on the provided criteria.
    /// </summary>
    private IQueryable<AuditLogEntry> BuildFilteredQuery(
        Guid? tenantId,
        Guid? userId,
        AuditActionType? actionType,
        string? entityType,
        string? entityId,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        IQueryable<AuditLogEntry> query = _dbContext.AuditLogEntries;

        if (tenantId.HasValue)
            query = query.Where(e => e.TenantId == tenantId.Value);

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        if (actionType.HasValue)
            query = query.Where(e => e.ActionType == actionType.Value);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(e => e.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(e => e.EntityId == entityId);

        if (fromUtc.HasValue)
            query = query.Where(e => e.TimestampUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(e => e.TimestampUtc <= toUtc.Value);

        return query;
    }
}
