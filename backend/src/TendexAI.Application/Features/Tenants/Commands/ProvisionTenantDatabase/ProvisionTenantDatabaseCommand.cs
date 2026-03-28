using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;

namespace TendexAI.Application.Features.Tenants.Commands.ProvisionTenantDatabase;

/// <summary>
/// Command to trigger automated database provisioning for a tenant.
/// Creates the isolated database, applies migrations, and seeds initial data.
/// </summary>
public sealed record ProvisionTenantDatabaseCommand(Guid TenantId) : ICommand<TenantDto>;
