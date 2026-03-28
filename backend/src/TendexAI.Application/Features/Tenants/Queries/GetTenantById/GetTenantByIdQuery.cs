using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;

namespace TendexAI.Application.Features.Tenants.Queries.GetTenantById;

/// <summary>
/// Query to retrieve a single tenant by its ID.
/// </summary>
public sealed record GetTenantByIdQuery(Guid TenantId) : IQuery<TenantDto>;
