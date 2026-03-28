using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FeatureFlags.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.FeatureFlags.Commands.CreateFeatureDefinition;

/// <summary>
/// Handles creating a new global feature definition.
/// </summary>
public sealed class CreateFeatureDefinitionCommandHandler
    : ICommandHandler<CreateFeatureDefinitionCommand, FeatureDefinitionDto>
{
    private readonly IFeatureDefinitionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateFeatureDefinitionCommandHandler> _logger;

    public CreateFeatureDefinitionCommandHandler(
        IFeatureDefinitionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateFeatureDefinitionCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<FeatureDefinitionDto>> Handle(
        CreateFeatureDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        // Check for duplicate feature key
        if (await _repository.ExistsByKeyAsync(request.FeatureKey, cancellationToken))
        {
            return Result.Failure<FeatureDefinitionDto>(
                $"A feature definition with key '{request.FeatureKey}' already exists.");
        }

        var featureDefinition = new FeatureDefinition(
            featureKey: request.FeatureKey,
            nameAr: request.NameAr,
            nameEn: request.NameEn,
            descriptionAr: request.DescriptionAr,
            descriptionEn: request.DescriptionEn,
            category: request.Category,
            isEnabledByDefault: request.IsEnabledByDefault);

        await _repository.AddAsync(featureDefinition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Feature definition '{FeatureKey}' created with ID {Id}.",
            featureDefinition.FeatureKey, featureDefinition.Id);

        return Result.Success(new FeatureDefinitionDto(
            Id: featureDefinition.Id,
            FeatureKey: featureDefinition.FeatureKey,
            NameAr: featureDefinition.NameAr,
            NameEn: featureDefinition.NameEn,
            DescriptionAr: featureDefinition.DescriptionAr,
            DescriptionEn: featureDefinition.DescriptionEn,
            Category: featureDefinition.Category,
            IsEnabledByDefault: featureDefinition.IsEnabledByDefault,
            IsActive: featureDefinition.IsActive,
            CreatedAt: featureDefinition.CreatedAt,
            LastModifiedAt: featureDefinition.LastModifiedAt));
    }
}
