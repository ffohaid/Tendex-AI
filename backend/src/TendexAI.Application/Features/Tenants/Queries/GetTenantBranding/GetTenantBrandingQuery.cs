using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;

namespace TendexAI.Application.Features.Tenants.Queries.GetTenantBranding;

/// <summary>
/// Query to retrieve the branding configuration for a specific tenant.
/// Used by the frontend to apply dynamic branding when a tenant user logs in.
/// </summary>
public sealed record GetTenantBrandingQuery(Guid TenantId) : IQuery<TenantBrandingDto>;
