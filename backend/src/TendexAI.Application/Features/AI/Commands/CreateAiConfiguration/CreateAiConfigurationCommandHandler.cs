using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.AI.Commands.CreateAiConfiguration;

/// <summary>
/// Handles the creation of a new AI configuration.
/// Encrypts the API key with AES-256 before persisting to the database.
/// </summary>
public sealed class CreateAiConfigurationCommandHandler
    : IRequestHandler<CreateAiConfigurationCommand, CreateAiConfigurationResult>
{
    private readonly IAiConfigurationRepository _repository;
    private readonly IAiKeyEncryptionService _encryptionService;
    private readonly ILogger<CreateAiConfigurationCommandHandler> _logger;

    public CreateAiConfigurationCommandHandler(
        IAiConfigurationRepository repository,
        IAiKeyEncryptionService encryptionService,
        ILogger<CreateAiConfigurationCommandHandler> logger)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<CreateAiConfigurationResult> Handle(
        CreateAiConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Encrypt the API key with AES-256 before storage
            var encryptedApiKey = _encryptionService.Encrypt(request.PlainApiKey);

            var configuration = new AiConfiguration(
                tenantId: request.TenantId,
                provider: request.Provider,
                modelName: request.ModelName,
                encryptedApiKey: encryptedApiKey,
                endpoint: request.Endpoint,
                qdrantCollectionName: request.QdrantCollectionName,
                maxTokens: request.MaxTokens,
                temperature: request.Temperature,
                priority: request.Priority,
                deploymentType: request.DeploymentType,
                description: request.Description);

            await _repository.AddAsync(configuration, cancellationToken);

            _logger.LogInformation(
                "Created AI configuration {ConfigId} for tenant {TenantId}, provider {Provider}",
                configuration.Id, request.TenantId, request.Provider);

            return new CreateAiConfigurationResult
            {
                ConfigurationId = configuration.Id,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create AI configuration for tenant {TenantId}",
                request.TenantId);

            return new CreateAiConfigurationResult
            {
                ConfigurationId = Guid.Empty,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
