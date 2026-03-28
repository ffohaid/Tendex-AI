using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FeatureFlags.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.FeatureFlags.Commands.ToggleFeatureFlag;

/// <summary>
/// Handles toggling a feature flag for a tenant.
/// If the flag doesn't exist for the tenant, it creates it.
/// If it exists, it updates the enabled state and configuration.
/// </summary>
public sealed class ToggleFeatureFlagCommandHandler
    : ICommandHandler<ToggleFeatureFlagCommand, TenantFeatureFlagDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantFeatureFlagRepository _featureFlagRepository;
    private readonly IFeatureDefinitionRepository _featureDefinitionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ToggleFeatureFlagCommandHandler> _logger;

    public ToggleFeatureFlagCommandHandler(
        ITenantRepository tenantRepository,
        ITenantFeatureFlagRepository featureFlagRepository,
        IFeatureDefinitionRepository featureDefinitionRepository,
        IUnitOfWork unitOfWork,
        ILogger<ToggleFeatureFlagCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _featureFlagRepository = featureFlagRepository;
        _featureDefinitionRepository = featureDefinitionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<TenantFeatureFlagDto>> Handle(
        ToggleFeatureFlagCommand request,
        CancellationToken cancellationToken)
    {
        // Verify tenant exists
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<TenantFeatureFlagDto>(
                $"Tenant with ID '{request.TenantId}' was not found.");
        }

        // Verify feature definition exists
        var featureDefinition = await _featureDefinitionRepository.GetByKeyAsync(
            request.FeatureKey, cancellationToken);
        if (featureDefinition is null)
        {
            return Result.Failure<TenantFeatureFlagDto>(
                $"Feature definition with key '{request.FeatureKey}' was not found.");
        }

        // Check if flag already exists for this tenant
        var existingFlag = await _featureFlagRepository.GetByTenantAndKeyAsync(
            request.TenantId, request.FeatureKey, cancellationToken);

        TenantFeatureFlag flag;

        if (existingFlag is not null)
        {
            // Update existing flag
            if (request.IsEnabled)
                existingFlag.Enable();
            else
                existingFlag.Disable();

            existingFlag.UpdateConfiguration(request.Configuration);
            await _featureFlagRepository.UpdateAsync(existingFlag, cancellationToken);
            flag = existingFlag;
        }
        else
        {
            // Create new flag
            flag = new TenantFeatureFlag(
                tenantId: request.TenantId,
                featureKey: request.FeatureKey,
                featureNameAr: featureDefinition.NameAr,
                featureNameEn: featureDefinition.NameEn,
                isEnabled: request.IsEnabled,
                configuration: request.Configuration);

            await _featureFlagRepository.AddAsync(flag, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Feature flag '{FeatureKey}' for tenant '{TenantId}' set to {IsEnabled}.",
            request.FeatureKey, request.TenantId, request.IsEnabled);

        return Result.Success(new TenantFeatureFlagDto(
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
}
