using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor that automatically captures entity changes
/// and writes immutable audit trail entries.
/// 
/// This interceptor:
/// - Detects Added, Modified, and Deleted entities
/// - Serializes old and new values as JSON
/// - Creates AuditLogEntry records (Append-Only)
/// - Excludes the AuditLogEntry entity itself to prevent infinite recursion
/// </summary>
public sealed class AuditTrailInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = 3
    };

    // Temporary storage for audit entries that need post-save processing (e.g., Added entities with DB-generated keys)
    private List<AuditEntry>? _pendingAuditEntries;

    public AuditTrailInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ProcessChanges(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ProcessChanges(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null && _pendingAuditEntries is { Count: > 0 })
        {
            await WritePendingAuditEntriesAsync(eventData.Context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        if (eventData.Context is not null && _pendingAuditEntries is { Count: > 0 })
        {
            WritePendingAuditEntriesAsync(eventData.Context, CancellationToken.None)
                .GetAwaiter().GetResult();
        }

        return base.SavedChanges(eventData, result);
    }

    /// <summary>
    /// Processes all tracked entity changes and creates audit entries.
    /// </summary>
    private void ProcessChanges(DbContext context)
    {
        context.ChangeTracker.DetectChanges();
        _pendingAuditEntries = [];

        var userId = _currentUserService.UserId ?? Guid.Empty;
        var userName = _currentUserService.UserName ?? "System";
        var ipAddress = _currentUserService.IpAddress;
        var sessionId = _currentUserService.SessionId;
        var tenantId = _currentUserService.TenantId;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Skip audit log entries themselves to prevent infinite recursion
            if (entry.Entity is AuditLogEntry)
                continue;

            // Skip entities that are not tracked or unchanged
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry
            {
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                SessionId = sessionId,
                TenantId = tenantId,
                EntityType = entry.Metadata.ClrType.Name,
                ActionType = entry.State switch
                {
                    EntityState.Added => AuditActionType.Create,
                    EntityState.Modified => AuditActionType.Update,
                    EntityState.Deleted => AuditActionType.Delete,
                    _ => AuditActionType.Access
                }
            };

            // Capture property values based on the change type
            foreach (var property in entry.Properties)
            {
                // Skip navigation properties and shadow properties
                if (property.IsTemporary)
                {
                    // Temporary values (e.g., auto-generated IDs) will be resolved after save
                    auditEntry.TemporaryProperties.Add(property);
                    continue;
                }

                var propertyName = property.Metadata.Name;

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        SetEntityId(auditEntry, entry, propertyName, property.CurrentValue);
                        break;

                    case EntityState.Deleted:
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        SetEntityId(auditEntry, entry, propertyName, property.OriginalValue);
                        break;

                    case EntityState.Modified:
                        if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                        {
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        SetEntityId(auditEntry, entry, propertyName, property.CurrentValue);
                        break;
                }
            }

            _pendingAuditEntries.Add(auditEntry);
        }
    }

    /// <summary>
    /// Writes pending audit entries to the database after the main SaveChanges completes.
    /// This ensures that auto-generated IDs (e.g., for Added entities) are available.
    /// </summary>
    private async Task WritePendingAuditEntriesAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (_pendingAuditEntries is null || _pendingAuditEntries.Count == 0)
            return;

        foreach (var auditEntry in _pendingAuditEntries)
        {
            // Resolve temporary properties (e.g., auto-generated IDs)
            foreach (var tempProperty in auditEntry.TemporaryProperties)
            {
                var propertyName = tempProperty.Metadata.Name;
                auditEntry.NewValues[propertyName] = tempProperty.CurrentValue;

                // If this is the primary key, set it as the entity ID
                if (tempProperty.Metadata.IsPrimaryKey())
                {
                    auditEntry.EntityId = tempProperty.CurrentValue?.ToString() ?? string.Empty;
                }
            }

            // Skip entries with no actual changes (for Modified state)
            if (auditEntry.ActionType == AuditActionType.Update
                && auditEntry.OldValues.Count == 0
                && auditEntry.NewValues.Count == 0)
            {
                continue;
            }

            var logEntry = new AuditLogEntry(
                userId: auditEntry.UserId,
                userName: auditEntry.UserName,
                ipAddress: auditEntry.IpAddress,
                actionType: auditEntry.ActionType,
                entityType: auditEntry.EntityType,
                entityId: auditEntry.EntityId,
                oldValues: auditEntry.OldValues.Count > 0
                    ? JsonSerializer.Serialize(auditEntry.OldValues, JsonOptions)
                    : null,
                newValues: auditEntry.NewValues.Count > 0
                    ? JsonSerializer.Serialize(auditEntry.NewValues, JsonOptions)
                    : null,
                reason: null,
                sessionId: auditEntry.SessionId,
                tenantId: auditEntry.TenantId);

            context.Set<AuditLogEntry>().Add(logEntry);
        }

        // Save audit entries in a separate SaveChanges call
        // The AuditTrailInterceptor will skip AuditLogEntry entities (checked above)
        await context.SaveChangesAsync(cancellationToken);

        _pendingAuditEntries.Clear();
    }

    /// <summary>
    /// Attempts to set the entity ID from the primary key property.
    /// </summary>
    private static void SetEntityId(AuditEntry auditEntry, EntityEntry entry, string propertyName, object? value)
    {
        if (string.IsNullOrEmpty(auditEntry.EntityId))
        {
            var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
            if (keyProperties is not null && keyProperties.Any(p => p.Name == propertyName))
            {
                auditEntry.EntityId = value?.ToString() ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// Internal class to temporarily hold audit data before persisting.
    /// </summary>
    private sealed class AuditEntry
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string? IpAddress { get; set; }
        public string? SessionId { get; set; }
        public Guid? TenantId { get; set; }
        public string EntityType { get; set; } = null!;
        public string EntityId { get; set; } = string.Empty;
        public AuditActionType ActionType { get; set; }
        public Dictionary<string, object?> OldValues { get; } = new();
        public Dictionary<string, object?> NewValues { get; } = new();
        public List<PropertyEntry> TemporaryProperties { get; } = [];
    }
}
