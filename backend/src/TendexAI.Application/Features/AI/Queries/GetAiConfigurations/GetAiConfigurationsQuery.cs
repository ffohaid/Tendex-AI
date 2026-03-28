using MediatR;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.AI.Queries.GetAiConfigurations;

/// <summary>
/// Query to retrieve all active AI configurations for a tenant.
/// </summary>
public sealed record GetAiConfigurationsQuery : IRequest<IReadOnlyList<AiConfigurationDto>>
{
    public required Guid TenantId { get; init; }
}

/// <summary>
/// DTO representing an AI configuration (API keys are never exposed).
/// </summary>
public sealed record AiConfigurationDto
{
    public required Guid Id { get; init; }
    public required Guid TenantId { get; init; }
    public required AiProvider Provider { get; init; }
    public required string ProviderName { get; init; }
    public required string ModelName { get; init; }
    public string? Endpoint { get; init; }
    public string? QdrantCollectionName { get; init; }
    public int MaxTokens { get; init; }
    public double Temperature { get; init; }
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public bool HasApiKey { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastModifiedAt { get; init; }
}
