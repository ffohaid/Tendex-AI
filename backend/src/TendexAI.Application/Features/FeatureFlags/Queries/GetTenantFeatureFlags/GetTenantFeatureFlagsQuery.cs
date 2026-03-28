using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FeatureFlags.Dtos;

namespace TendexAI.Application.Features.FeatureFlags.Queries.GetTenantFeatureFlags;

/// <summary>
/// Query to retrieve all feature flags for a specific tenant.
/// </summary>
public sealed record GetTenantFeatureFlagsQuery(Guid TenantId)
    : IQuery<IReadOnlyList<TenantFeatureFlagDto>>;
