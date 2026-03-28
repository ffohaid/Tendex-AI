using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Tenants.Commands.ChangeTenantStatus;

/// <summary>
/// Command to change a tenant's lifecycle status.
/// Enforces valid state transitions as defined in the PRD.
/// </summary>
public sealed record ChangeTenantStatusCommand(
    Guid TenantId,
    TenantStatus NewStatus) : ICommand<TenantDto>;
