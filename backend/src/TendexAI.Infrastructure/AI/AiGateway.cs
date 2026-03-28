using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.AI;

/// <summary>
/// Unified AI Gateway that routes requests to the appropriate AI provider
/// based on tenant configuration. Supports dynamic model switching by
/// selecting the highest-priority active provider for each tenant.
///
/// Architecture:
/// - Fetches AI configuration from the AiConfiguration table (master_platform DB).
/// - Decrypts API keys in-memory only during execution (AES-256).
/// - Routes to the correct provider client (OpenAI, Anthropic, Azure OpenAI, Local).
/// - Supports fallback: if the preferred provider fails, tries the next active provider.
/// </summary>
public sealed class AiGateway : IAiGateway
{
    private readonly IAiConfigurationRepository _configRepository;
    private readonly IAiKeyEncryptionService _encryptionService;
    private readonly IEnumerable<IAiProviderClient> _providerClients;
    private readonly ILogger<AiGateway> _logger;

    public AiGateway(
        IAiConfigurationRepository configRepository,
        IAiKeyEncryptionService encryptionService,
        IEnumerable<IAiProviderClient> providerClients,
        ILogger<AiGateway> logger)
    {
        _configRepository = configRepository;
        _encryptionService = encryptionService;
        _providerClients = providerClients;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AiCompletionResponse> GenerateCompletionAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Resolve the AI configuration(s) for this tenant
        var configurations = await ResolveConfigurationsAsync(
            request.TenantId,
            request.PreferredProvider,
            cancellationToken);

        if (configurations.Count == 0)
        {
            _logger.LogWarning(
                "No active AI configuration found for tenant {TenantId}",
                request.TenantId);

            return AiCompletionResponse.Failure(
                "No active AI configuration found for this tenant.",
                request.PreferredProvider ?? AiProvider.OpenAI,
                "unknown");
        }

        // 2. Try each configuration in priority order (supports fallback)
        foreach (var config in configurations)
        {
            var providerClient = GetProviderClient(config.Provider);
            if (providerClient is null)
            {
                _logger.LogWarning(
                    "No provider client registered for {Provider}. Skipping.",
                    config.Provider);
                continue;
            }

            // 3. Decrypt API key in-memory only
            string decryptedApiKey;
            try
            {
                decryptedApiKey = _encryptionService.Decrypt(config.EncryptedApiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to decrypt API key for configuration {ConfigId}",
                    config.Id);
                continue;
            }

            try
            {
                // 4. Build the effective prompt with RAG context if provided
                var effectiveUserPrompt = BuildEffectivePrompt(request);

                var response = await providerClient.SendCompletionAsync(
                    apiKey: decryptedApiKey,
                    endpoint: config.Endpoint,
                    modelName: request.ModelNameOverride ?? config.ModelName,
                    systemPrompt: request.SystemPrompt,
                    userPrompt: effectiveUserPrompt,
                    conversationHistory: request.ConversationHistory,
                    maxTokens: request.MaxTokensOverride ?? config.MaxTokens,
                    temperature: request.TemperatureOverride ?? config.Temperature,
                    cancellationToken: cancellationToken);

                if (response.IsSuccess)
                {
                    _logger.LogInformation(
                        "AI completion successful via {Provider}/{Model} for tenant {TenantId}. " +
                        "Tokens: {PromptTokens}+{CompletionTokens}={TotalTokens}, Latency: {LatencyMs}ms",
                        config.Provider, config.ModelName, request.TenantId,
                        response.PromptTokens, response.CompletionTokens,
                        response.TotalTokens, response.LatencyMs);

                    return response;
                }

                _logger.LogWarning(
                    "AI completion failed via {Provider}/{Model}: {Error}. Trying next provider.",
                    config.Provider, config.ModelName, response.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception during AI completion via {Provider}/{Model}",
                    config.Provider, config.ModelName);
            }
            finally
            {
                // Clear the decrypted key from memory as soon as possible
                decryptedApiKey = null!;
            }
        }

        return AiCompletionResponse.Failure(
            "All configured AI providers failed for this tenant.",
            request.PreferredProvider ?? configurations[0].Provider,
            configurations[0].ModelName);
    }

    /// <inheritdoc />
    public async Task<AiEmbeddingResponse> GenerateEmbeddingAsync(
        AiEmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var configurations = await ResolveConfigurationsAsync(
            request.TenantId,
            request.PreferredProvider,
            cancellationToken);

        if (configurations.Count == 0)
        {
            return AiEmbeddingResponse.Failure(
                "No active AI configuration found for this tenant.",
                request.PreferredProvider ?? AiProvider.OpenAI,
                "unknown");
        }

        foreach (var config in configurations)
        {
            var providerClient = GetProviderClient(config.Provider);
            if (providerClient is null) continue;

            string decryptedApiKey;
            try
            {
                decryptedApiKey = _encryptionService.Decrypt(config.EncryptedApiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to decrypt API key for embedding configuration {ConfigId}",
                    config.Id);
                continue;
            }

            try
            {
                var response = await providerClient.SendEmbeddingAsync(
                    apiKey: decryptedApiKey,
                    endpoint: config.Endpoint,
                    modelName: request.ModelNameOverride ?? config.ModelName,
                    text: request.Text,
                    cancellationToken: cancellationToken);

                if (response.IsSuccess)
                {
                    _logger.LogInformation(
                        "Embedding generation successful via {Provider}/{Model} for tenant {TenantId}. " +
                        "Dimensions: {Dimensions}",
                        config.Provider, config.ModelName, request.TenantId,
                        response.Dimensions);

                    return response;
                }

                _logger.LogWarning(
                    "Embedding generation failed via {Provider}/{Model}: {Error}. Trying next provider.",
                    config.Provider, config.ModelName, response.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception during embedding generation via {Provider}/{Model}",
                    config.Provider, config.ModelName);
            }
            finally
            {
                decryptedApiKey = null!;
            }
        }

        return AiEmbeddingResponse.Failure(
            "All configured AI providers failed for embedding generation.",
            request.PreferredProvider ?? configurations[0].Provider,
            configurations[0].ModelName);
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var configurations = await _configRepository
            .GetAllActiveConfigurationsAsync(tenantId, cancellationToken);

        return configurations.Count > 0;
    }

    // ----- Private Methods -----

    /// <summary>
    /// Resolves the AI configurations for a tenant, ordered by priority (descending).
    /// If a preferred provider is specified, it is placed first.
    /// </summary>
    private async Task<IReadOnlyList<AiConfiguration>> ResolveConfigurationsAsync(
        Guid tenantId,
        AiProvider? preferredProvider,
        CancellationToken cancellationToken)
    {
        var allConfigs = await _configRepository
            .GetAllActiveConfigurationsAsync(tenantId, cancellationToken);

        if (allConfigs.Count == 0)
            return allConfigs;

        // Sort by priority descending
        var sorted = allConfigs.OrderByDescending(c => c.Priority).ToList();

        // If a preferred provider is specified, move it to the front
        if (preferredProvider.HasValue)
        {
            var preferred = sorted
                .Where(c => c.Provider == preferredProvider.Value)
                .ToList();

            var others = sorted
                .Where(c => c.Provider != preferredProvider.Value)
                .ToList();

            sorted = [.. preferred, .. others];
        }

        return sorted;
    }

    /// <summary>
    /// Gets the provider client for the specified AI provider.
    /// </summary>
    private IAiProviderClient? GetProviderClient(AiProvider provider)
    {
        return _providerClients.FirstOrDefault(c => c.Provider == provider);
    }

    /// <summary>
    /// Builds the effective user prompt by incorporating RAG context if available.
    /// </summary>
    private static string BuildEffectivePrompt(AiCompletionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RagContext))
            return request.UserPrompt;

        return $"""
            السياق المرجعي (من المستندات المعتمدة):
            ---
            {request.RagContext}
            ---

            السؤال/الطلب:
            {request.UserPrompt}

            تعليمات: أجب بناءً على السياق المرجعي أعلاه فقط. إذا لم تجد الإجابة في السياق، أوضح ذلك صراحةً. لا تولّد معلومات غير موجودة في المستندات المرجعية.
            """;
    }
}
