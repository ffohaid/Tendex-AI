using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.DeleteAiConfiguration;

/// <summary>
/// Handler for soft-deleting (deactivating) an AI configuration.
/// Sets IsActive = false instead of physically removing the record.
/// </summary>
public sealed class DeleteAiConfigurationCommandHandler
    : IRequestHandler<DeleteAiConfigurationCommand, bool>
{
    private readonly IAiConfigurationRepository _repository;
    private readonly ILogger<DeleteAiConfigurationCommandHandler> _logger;

    public DeleteAiConfigurationCommandHandler(
        IAiConfigurationRepository repository,
        ILogger<DeleteAiConfigurationCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        DeleteAiConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        var configuration = await _repository.GetByIdAsync(
            request.ConfigurationId, cancellationToken);

        if (configuration is null)
        {
            _logger.LogWarning(
                "AI configuration {ConfigId} not found for deletion",
                request.ConfigurationId);
            return false;
        }

        configuration.Deactivate();
        await _repository.UpdateAsync(configuration, cancellationToken);

        _logger.LogInformation(
            "AI configuration {ConfigId} deactivated (soft deleted) for tenant {TenantId}",
            configuration.Id, configuration.TenantId);

        return true;
    }
}
