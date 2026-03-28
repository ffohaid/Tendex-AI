using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.UpdateAiConfiguration;

/// <summary>
/// Handles updating an existing AI configuration's model settings.
/// </summary>
public sealed class UpdateAiConfigurationCommandHandler
    : IRequestHandler<UpdateAiConfigurationCommand, bool>
{
    private readonly IAiConfigurationRepository _repository;
    private readonly ILogger<UpdateAiConfigurationCommandHandler> _logger;

    public UpdateAiConfigurationCommandHandler(
        IAiConfigurationRepository repository,
        ILogger<UpdateAiConfigurationCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        UpdateAiConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        var config = await _repository.GetByIdAsync(request.ConfigurationId, cancellationToken);

        if (config is null)
        {
            _logger.LogWarning(
                "AI configuration {ConfigId} not found for update",
                request.ConfigurationId);
            return false;
        }

        config.UpdateModelSettings(
            modelName: request.ModelName,
            endpoint: request.Endpoint,
            maxTokens: request.MaxTokens,
            temperature: request.Temperature,
            priority: request.Priority);

        await _repository.UpdateAsync(config, cancellationToken);

        _logger.LogInformation(
            "Updated AI configuration {ConfigId} model settings",
            request.ConfigurationId);

        return true;
    }
}
