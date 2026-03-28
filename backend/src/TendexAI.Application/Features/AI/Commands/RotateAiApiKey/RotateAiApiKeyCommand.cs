using MediatR;

namespace TendexAI.Application.Features.AI.Commands.RotateAiApiKey;

/// <summary>
/// Command to rotate (update) the API key for an AI configuration.
/// The new key will be encrypted with AES-256 before storage.
/// </summary>
public sealed record RotateAiApiKeyCommand : IRequest<bool>
{
    public required Guid ConfigurationId { get; init; }
    public required string NewPlainApiKey { get; init; }
}
