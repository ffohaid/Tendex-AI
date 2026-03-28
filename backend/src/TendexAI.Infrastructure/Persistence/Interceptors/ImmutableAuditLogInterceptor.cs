using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor that enforces the immutability constraint
/// on <see cref="AuditLogEntry"/> entities.
/// 
/// This interceptor runs BEFORE SaveChanges and throws an <see cref="InvalidOperationException"/>
/// if any attempt is made to UPDATE or DELETE an audit log entry.
/// 
/// This is a critical security control aligned with PRD v6 Section 20:
/// "The application database user MUST NOT have UPDATE or DELETE permissions on the audit log table."
/// </summary>
public sealed class ImmutableAuditLogInterceptor : SaveChangesInterceptor
{
    private const string ViolationMessage =
        "SECURITY VIOLATION: Audit log entries are immutable. " +
        "UPDATE and DELETE operations on AuditLogEntry are strictly prohibited. " +
        "This incident has been logged.";

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            EnforceImmutability(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            EnforceImmutability(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Scans the change tracker for any Modified or Deleted <see cref="AuditLogEntry"/> entities
    /// and throws an exception if found, preventing the operation from proceeding.
    /// </summary>
    private static void EnforceImmutability(DbContext context)
    {
        var violatingEntries = context.ChangeTracker
            .Entries<AuditLogEntry>()
            .Where(e => e.State is EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (violatingEntries.Count > 0)
        {
            // Revert the state to prevent any partial saves
            foreach (var entry in violatingEntries)
            {
                entry.State = EntityState.Unchanged;
            }

            throw new InvalidOperationException(ViolationMessage);
        }
    }
}
