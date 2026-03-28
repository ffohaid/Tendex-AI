using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FeatureFlags.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.FeatureFlags.Queries.GetTenantFeatureFlags;

/// <summary>
/// Handles retrieving all feature flags for a specific tenant.
/// </summary>
public sealed class GetTenantFeatureFlagsQueryHandler
    : IQueryHandler<GetTenantFeatureFlagsQuery, IReadOnlyList<TenantFeatureFlagDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantFeatureFlagRepository _featureFlagRepository;

    public GetTenantFeatureFlagsQueryHandler(
        ITenantRepository tenantRepository,
        ITenantFeatureFlagRepository featureFlagRepository)
    {
        _tenantRepository = tenantRepository;
        _featureFlagRepository = featureFlagRepository;
    }

    public async Task<Result<IReadOnlyList<TenantFeatureFlagDto>>> Handle(
        GetTenantFeatureFlagsQuery request,
        CancellationToken cancellationToken)
    {
        // Verify tenant exists
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<IReadOnlyList<TenantFeatureFlagDto>>(
                $"Tenant with ID '{request.TenantId}' was not found.");
        }

        var flags = await _featureFlagRepository.GetByTenantIdAsync(
            request.TenantId, cancellationToken);

        var dtos = flags.Select(f => new TenantFeatureFlagDto(
            Id: f.Id,
            TenantId: f.TenantId,
            FeatureKey: f.FeatureKey,
            FeatureNameAr: f.FeatureNameAr,
            FeatureNameEn: f.FeatureNameEn,
            IsEnabled: f.IsEnabled,
            Configuration: f.Configuration,
            CreatedAt: f.CreatedAt,
            LastModifiedAt: f.LastModifiedAt)).ToList();

        return Result.Success<IReadOnlyList<TenantFeatureFlagDto>>(dtos);
    }
}
