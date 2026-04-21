using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.TestAiConnection;

/// <summary>
/// Handles testing an AI configuration's connectivity.
/// Performs three checks:
/// 1. Configuration exists and is active
/// 2. API key can be decrypted successfully
/// 3. Provider responds to a minimal test prompt
/// </summary>
public sealed class TestAiConnectionCommandHandler
    : IRequestHandler<TestAiConnectionCommand, TestAiConnectionResult>
{
    private readonly IAiConfigurationRepository _repository;
    private readonly IAiKeyEncryptionService _encryptionService;
    private readonly IEnumerable<IAiProviderClient> _providerClients;
    private readonly ILogger<TestAiConnectionCommandHandler> _logger;

    public TestAiConnectionCommandHandler(
        IAiConfigurationRepository repository,
        IAiKeyEncryptionService encryptionService,
        IEnumerable<IAiProviderClient> providerClients,
        ILogger<TestAiConnectionCommandHandler> logger)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _providerClients = providerClients;
        _logger = logger;
    }

    public async Task<TestAiConnectionResult> Handle(
        TestAiConnectionCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find the configuration
        var config = await _repository.GetByIdAsync(request.ConfigurationId, cancellationToken);
        if (config is null)
        {
            return TestAiConnectionResult.Failure("AI configuration not found.");
        }

        var providerName = config.Provider.ToString();
        var modelName = config.ModelName;

        // 2. Test decryption
        string decryptedApiKey;
        try
        {
            decryptedApiKey = _encryptionService.Decrypt(config.EncryptedApiKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "API key decryption failed for configuration {ConfigId}. " +
                "The key may have been encrypted with a different master key.",
                config.Id);

            return TestAiConnectionResult.Failure(
                "Failed to decrypt API key. The key may need to be re-entered (rotated).",
                providerName, modelName);
        }

        // 3. Find the provider client
        var providerClient = _providerClients.FirstOrDefault(c => c.Provider == config.Provider);
        if (providerClient is null)
        {
            return TestAiConnectionResult.Failure(
                $"No provider client registered for {config.Provider}.",
                providerName, modelName);
        }

        // 4. Send a minimal test prompt
        try
        {
            var sw = Stopwatch.StartNew();

            var response = await providerClient.SendCompletionAsync(
                apiKey: decryptedApiKey,
                endpoint: config.Endpoint,
                modelName: config.ModelName,
                systemPrompt: "You are a test assistant.",
                userPrompt: "Reply with exactly: OK",
                conversationHistory: null,
                maxTokens: 10,
                temperature: 0,
                cancellationToken: cancellationToken);

            sw.Stop();

            if (response.IsSuccess)
            {
                _logger.LogInformation(
                    "AI connection test successful for {ConfigId} ({Provider}/{Model}) in {LatencyMs}ms",
                    config.Id, providerName, modelName, sw.ElapsedMilliseconds);

                return TestAiConnectionResult.Success(
                    providerName, modelName, (int)sw.ElapsedMilliseconds);
            }

            return TestAiConnectionResult.Failure(
                $"Provider responded with error: {response.ErrorMessage}",
                providerName, modelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AI connection test failed for {ConfigId} ({Provider}/{Model})",
                config.Id, providerName, modelName);

            return TestAiConnectionResult.Failure(
                $"Connection failed: {ex.Message}",
                providerName, modelName);
        }
        finally
        {
            decryptedApiKey = null!;
        }
    }
}
