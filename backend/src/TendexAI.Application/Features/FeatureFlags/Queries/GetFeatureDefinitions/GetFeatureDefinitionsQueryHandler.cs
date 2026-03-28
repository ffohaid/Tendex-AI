using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FeatureFlags.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.FeatureFlags.Queries.GetFeatureDefinitions;

/// <summary>
/// Handles retrieving all active feature definitions.
/// </summary>
public sealed class GetFeatureDefinitionsQueryHandler
    : IQueryHandler<GetFeatureDefinitionsQuery, IReadOnlyList<FeatureDefinitionDto>>
{
    private readonly IFeatureDefinitionRepository _repository;

    public GetFeatureDefinitionsQueryHandler(IFeatureDefinitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<FeatureDefinitionDto>>> Handle(
        GetFeatureDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _repository.GetAllActiveAsync(cancellationToken);

        var dtos = definitions.Select(d => new FeatureDefinitionDto(
            Id: d.Id,
            FeatureKey: d.FeatureKey,
            NameAr: d.NameAr,
            NameEn: d.NameEn,
            DescriptionAr: d.DescriptionAr,
            DescriptionEn: d.DescriptionEn,
            Category: d.Category,
            IsEnabledByDefault: d.IsEnabledByDefault,
            IsActive: d.IsActive,
            CreatedAt: d.CreatedAt,
            LastModifiedAt: d.LastModifiedAt)).ToList();

        return Result.Success<IReadOnlyList<FeatureDefinitionDto>>(dtos);
    }
}
