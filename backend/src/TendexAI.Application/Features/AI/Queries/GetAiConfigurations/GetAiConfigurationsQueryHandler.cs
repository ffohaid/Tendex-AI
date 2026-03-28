using MediatR;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Queries.GetAiConfigurations;

/// <summary>
/// Handles retrieving all active AI configurations for a tenant.
/// API keys are never exposed in the response.
/// </summary>
public sealed class GetAiConfigurationsQueryHandler
    : IRequestHandler<GetAiConfigurationsQuery, IReadOnlyList<AiConfigurationDto>>
{
    private readonly IAiConfigurationRepository _repository;

    public GetAiConfigurationsQueryHandler(IAiConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<AiConfigurationDto>> Handle(
        GetAiConfigurationsQuery request,
        CancellationToken cancellationToken)
    {
        var configurations = await _repository
            .GetAllActiveConfigurationsAsync(request.TenantId, cancellationToken);

        return configurations.Select(c => new AiConfigurationDto
        {
            Id = c.Id,
            TenantId = c.TenantId,
            Provider = c.Provider,
            ProviderName = c.Provider.ToString(),
            ModelName = c.ModelName,
            Endpoint = c.Endpoint,
            QdrantCollectionName = c.QdrantCollectionName,
            MaxTokens = c.MaxTokens,
            Temperature = c.Temperature,
            Priority = c.Priority,
            IsActive = c.IsActive,
            HasApiKey = !string.IsNullOrWhiteSpace(c.EncryptedApiKey),
            CreatedAt = c.CreatedAt,
            LastModifiedAt = c.LastModifiedAt
        }).ToList();
    }
}
