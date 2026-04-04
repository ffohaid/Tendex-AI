using MediatR;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.AI.Commands.CreateAiConfiguration;

/// <summary>
/// Command to create a new AI configuration for a tenant.
/// The API key will be encrypted with AES-256 before storage.
/// </summary>
public sealed record CreateAiConfigurationCommand : IRequest<CreateAiConfigurationResult>
{
    public required Guid TenantId { get; init; }
    public required AiProvider Provider { get; init; }
    public required string ModelName { get; init; }
    public required string PlainApiKey { get; init; }
    public string? Endpoint { get; init; }
    public string? QdrantCollectionName { get; init; }
    public int MaxTokens { get; init; } = 4096;
    public double Temperature { get; init; } = 0.3;
    public int Priority { get; init; }
    public AiDeploymentType DeploymentType { get; init; } = AiDeploymentType.PublicCloud;
    public string? Description { get; init; }
}

public sealed record CreateAiConfigurationResult
{
    public required Guid ConfigurationId { get; init; }
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
