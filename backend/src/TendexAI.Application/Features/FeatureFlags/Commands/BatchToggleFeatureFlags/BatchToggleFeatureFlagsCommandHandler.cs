using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FeatureFlags.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.FeatureFlags.Commands.BatchToggleFeatureFlags;

/// <summary>
/// Handles batch-toggling multiple feature flags for a tenant.
/// For each flag item: creates the flag if it doesn't exist, or updates it if it does.
/// All changes are committed in a single transaction.
/// </summary>
public sealed class BatchToggleFeatureFlagsCommandHandler
    : ICommandHandler<BatchToggleFeatureFlagsCommand, IReadOnlyList<TenantFeatureFlagDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantFeatureFlagRepository _featureFlagRepository;
    private readonly IFeatureDefinitionRepository _featureDefinitionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BatchToggleFeatureFlagsCommandHandler> _logger;

    public BatchToggleFeatureFlagsCommandHandler(
        ITenantRepository tenantRepository,
        ITenantFeatureFlagRepository featureFlagRepository,
        IFeatureDefinitionRepository featureDefinitionRepository,
        IUnitOfWork unitOfWork,
        ILogger<BatchToggleFeatureFlagsCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _featureFlagRepository = featureFlagRepository;
        _featureDefinitionRepository = featureDefinitionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<TenantFeatureFlagDto>>> Handle(
        BatchToggleFeatureFlagsCommand request,
        CancellationToken cancellationToken)
    {
        // Verify tenant exists
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<IReadOnlyList<TenantFeatureFlagDto>>(
                $"Tenant with ID '{request.TenantId}' was not found.");
        }

        var results = new List<TenantFeatureFlagDto>();

        foreach (var item in request.Flags)
        {
            // Verify feature definition exists
            var featureDefinition = await _featureDefinitionRepository.GetByKeyAsync(
                item.FeatureKey, cancellationToken);
            if (featureDefinition is null)
            {
                return Result.Failure<IReadOnlyList<TenantFeatureFlagDto>>(
                    $"Feature definition with key '{item.FeatureKey}' was not found.");
            }

            // Check if flag already exists for this tenant
            var existingFlag = await _featureFlagRepository.GetByTenantAndKeyAsync(
                request.TenantId, item.FeatureKey, cancellationToken);

            TenantFeatureFlag flag;

            if (existingFlag is not null)
            {
                // Update existing flag
                if (item.IsEnabled)
                    existingFlag.Enable();
                else
                    existingFlag.Disable();

                existingFlag.UpdateConfiguration(item.Configuration);
                await _featureFlagRepository.UpdateAsync(existingFlag, cancellationToken);
                flag = existingFlag;
            }
            else
            {
                // Create new flag
                flag = new TenantFeatureFlag(
                    tenantId: request.TenantId,
                    featureKey: item.FeatureKey,
                    featureNameAr: featureDefinition.NameAr,
                    featureNameEn: featureDefinition.NameEn,
                    isEnabled: item.IsEnabled,
                    configuration: item.Configuration);

                await _featureFlagRepository.AddAsync(flag, cancellationToken);
            }

            results.Add(new TenantFeatureFlagDto(
                Id: flag.Id,
                TenantId: flag.TenantId,
                FeatureKey: flag.FeatureKey,
                FeatureNameAr: flag.FeatureNameAr,
                FeatureNameEn: flag.FeatureNameEn,
                IsEnabled: flag.IsEnabled,
                Configuration: flag.Configuration,
                CreatedAt: flag.CreatedAt,
                LastModifiedAt: flag.LastModifiedAt));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Batch-toggled {Count} feature flags for tenant '{TenantId}'.",
            request.Flags.Count, request.TenantId);

        return Result.Success<IReadOnlyList<TenantFeatureFlagDto>>(results);
    }
}
