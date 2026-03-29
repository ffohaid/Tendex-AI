using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Notifications;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Notification entity.
/// Operates against the tenant-specific database.
/// </summary>
public sealed class NotificationRepository : INotificationRepository
{
    private readonly TenantDbContext _dbContext;

    public NotificationRepository(ITenantDbContextFactory dbContextFactory)
    {
        _dbContext = dbContextFactory.CreateDbContext();
    }

    /// <inheritdoc />
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Notification> Items, int TotalCount, int UnreadCount)> GetPagedByUserAsync(
        Guid tenantId,
        Guid userId,
        int pageNumber,
        int pageSize,
        bool? isReadFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(n => n.TenantId == tenantId && n.UserId == userId);

        if (isReadFilter.HasValue)
        {
            query = query.Where(n => n.IsRead == isReadFilter.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var unreadCount = await _dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(n => n.TenantId == tenantId && n.UserId == userId && !n.IsRead)
            .CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount, unreadCount);
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(n => n.TenantId == tenantId && n.UserId == userId && !n.IsRead)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Notification>().AddAsync(notification, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Notification notification)
    {
        _dbContext.Set<Notification>().Update(notification);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
