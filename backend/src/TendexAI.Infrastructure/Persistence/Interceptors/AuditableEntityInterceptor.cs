using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TendexAI.Domain.Common;

namespace TendexAI.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor that automatically populates
/// audit fields (CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy)
/// on entities inheriting from <see cref="BaseEntity{TId}"/>.
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            UpdateAuditFields(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            UpdateAuditFields(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private static void UpdateAuditFields(DbContext context)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity<Guid>>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    // CreatedBy will be set from the current user context in a future sprint
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedAt = utcNow;
                    // LastModifiedBy will be set from the current user context in a future sprint
                    break;
            }
        }
    }
}
