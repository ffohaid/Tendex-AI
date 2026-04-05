using MediatR;

namespace TendexAI.Application.Features.AI.Commands.DeleteAiConfiguration;

/// <summary>
/// Command to soft-delete (deactivate) an AI configuration.
/// </summary>
public sealed class DeleteAiConfigurationCommand : IRequest<bool>
{
    /// <summary>
    /// The unique identifier of the AI configuration to deactivate.
    /// </summary>
    public Guid ConfigurationId { get; init; }
}
