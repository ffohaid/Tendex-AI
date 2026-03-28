using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.RotateAiApiKey;

/// <summary>
/// Handles rotating the API key for an AI configuration.
/// Encrypts the new key with AES-256 before persisting.
/// </summary>
public sealed class RotateAiApiKeyCommandHandler
    : IRequestHandler<RotateAiApiKeyCommand, bool>
{
    private readonly IAiConfigurationRepository _repository;
    private readonly IAiKeyEncryptionService _encryptionService;
    private readonly ILogger<RotateAiApiKeyCommandHandler> _logger;

    public RotateAiApiKeyCommandHandler(
        IAiConfigurationRepository repository,
        IAiKeyEncryptionService encryptionService,
        ILogger<RotateAiApiKeyCommandHandler> logger)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<bool> Handle(
        RotateAiApiKeyCommand request,
        CancellationToken cancellationToken)
    {
        var config = await _repository.GetByIdAsync(request.ConfigurationId, cancellationToken);

        if (config is null)
        {
            _logger.LogWarning(
                "AI configuration {ConfigId} not found for API key rotation",
                request.ConfigurationId);
            return false;
        }

        var encryptedKey = _encryptionService.Encrypt(request.NewPlainApiKey);
        config.UpdateApiKey(encryptedKey);

        await _repository.UpdateAsync(config, cancellationToken);

        _logger.LogInformation(
            "Rotated API key for AI configuration {ConfigId}",
            request.ConfigurationId);

        return true;
    }
}
