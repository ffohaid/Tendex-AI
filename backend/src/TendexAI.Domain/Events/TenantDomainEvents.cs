using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Events;

/// <summary>
/// Domain event raised when a new tenant is created and needs provisioning.
/// </summary>
public sealed record TenantCreatedDomainEvent(
    Guid TenantId,
    string Identifier,
    string DatabaseName) : IDomainEvent;

/// <summary>
/// Domain event raised when a tenant's database has been provisioned.
/// </summary>
public sealed record TenantProvisionedDomainEvent(
    Guid TenantId,
    string DatabaseName) : IDomainEvent;

/// <summary>
/// Domain event raised when a tenant's status changes.
/// </summary>
public sealed record TenantStatusChangedDomainEvent(
    Guid TenantId,
    TenantStatus OldStatus,
    TenantStatus NewStatus) : IDomainEvent;
