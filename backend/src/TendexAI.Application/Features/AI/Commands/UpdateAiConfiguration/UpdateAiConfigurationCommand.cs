using MediatR;

namespace TendexAI.Application.Features.AI.Commands.UpdateAiConfiguration;

/// <summary>
/// Command to update an existing AI configuration's model settings.
/// </summary>
public sealed record UpdateAiConfigurationCommand : IRequest<bool>
{
    public required Guid ConfigurationId { get; init; }
    public required string ModelName { get; init; }
    public string? Endpoint { get; init; }
    public int MaxTokens { get; init; } = 4096;
    public double Temperature { get; init; } = 0.3;
    public int Priority { get; init; } = 0;
}
