using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.AI.Providers;

/// <summary>
/// AI provider client for Azure OpenAI Service.
/// Uses Azure-specific authentication (api-key header) and endpoint format.
/// </summary>
public sealed class AzureOpenAiProviderClient : IAiProviderClient
{
    private const string DefaultApiVersion = "2024-06-01";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AzureOpenAiProviderClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AzureOpenAiProviderClient(
        IHttpClientFactory httpClientFactory,
        ILogger<AzureOpenAiProviderClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public AiProvider Provider => AiProvider.AzureOpenAI;

    /// <inheritdoc />
    public async Task<AiCompletionResponse> SendCompletionAsync(
        string apiKey,
        string? endpoint,
        string modelName,
        string? systemPrompt,
        string userPrompt,
        IReadOnlyList<AiChatMessage>? conversationHistory,
        int maxTokens,
        double temperature,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return AiCompletionResponse.Failure(
                "Azure OpenAI endpoint is required but not configured.",
                AiProvider.AzureOpenAI,
                modelName);
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var messages = BuildMessages(systemPrompt, userPrompt, conversationHistory);

            var requestBody = new AzureChatRequest
            {
                Messages = messages,
                MaxTokens = maxTokens,
                Temperature = temperature
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);

            using var client = _httpClientFactory.CreateClient("AzureOpenAI");
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            // Azure OpenAI URL format: {endpoint}/openai/deployments/{deployment-name}/chat/completions?api-version={version}
            var requestUrl = $"{endpoint.TrimEnd('/')}/openai/deployments/{modelName}/chat/completions?api-version={DefaultApiVersion}";

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(requestUrl, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Azure OpenAI API returned {StatusCode} for deployment {Model}: {Body}",
                    response.StatusCode, modelName, responseBody);

                return AiCompletionResponse.Failure(
                    $"Azure OpenAI API error: {response.StatusCode} - {responseBody}",
                    AiProvider.AzureOpenAI,
                    modelName);
            }

            var result = JsonSerializer.Deserialize<AzureChatResponse>(responseBody, JsonOptions);

            sw.Stop();

            var completionContent = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

            return AiCompletionResponse.Success(
                content: completionContent,
                provider: AiProvider.AzureOpenAI,
                modelName: modelName,
                promptTokens: result?.Usage?.PromptTokens ?? 0,
                completionTokens: result?.Usage?.CompletionTokens ?? 0,
                latencyMs: sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Azure OpenAI API for deployment {Model}", modelName);
            return AiCompletionResponse.Failure(
                $"Azure OpenAI API exception: {ex.Message}",
                AiProvider.AzureOpenAI,
                modelName);
        }
    }

    /// <inheritdoc />
    public async Task<AiEmbeddingResponse> SendEmbeddingAsync(
        string apiKey,
        string? endpoint,
        string modelName,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return AiEmbeddingResponse.Failure(
                "Azure OpenAI endpoint is required but not configured.",
                AiProvider.AzureOpenAI,
                modelName);
        }

        try
        {
            var requestBody = new AzureEmbeddingRequest { Input = text };
            var json = JsonSerializer.Serialize(requestBody, JsonOptions);

            using var client = _httpClientFactory.CreateClient("AzureOpenAI");
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            var requestUrl = $"{endpoint.TrimEnd('/')}/openai/deployments/{modelName}/embeddings?api-version={DefaultApiVersion}";

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(requestUrl, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Azure OpenAI Embedding API returned {StatusCode}: {Body}",
                    response.StatusCode, responseBody);

                return AiEmbeddingResponse.Failure(
                    $"Azure OpenAI Embedding API error: {response.StatusCode}",
                    AiProvider.AzureOpenAI,
                    modelName);
            }

            var result = JsonSerializer.Deserialize<AzureEmbeddingResponse>(responseBody, JsonOptions);
            var embedding = result?.Data?.FirstOrDefault()?.Embedding ?? [];

            return AiEmbeddingResponse.Success(
                embedding: embedding,
                provider: AiProvider.AzureOpenAI,
                modelName: modelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Azure OpenAI Embedding API for deployment {Model}", modelName);
            return AiEmbeddingResponse.Failure(
                $"Azure OpenAI Embedding API exception: {ex.Message}",
                AiProvider.AzureOpenAI,
                modelName);
        }
    }

    // ----- Helper Methods -----

    private static List<AzureMessage> BuildMessages(
        string? systemPrompt,
        string userPrompt,
        IReadOnlyList<AiChatMessage>? conversationHistory)
    {
        var messages = new List<AzureMessage>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new AzureMessage { Role = "system", Content = systemPrompt });
        }

        if (conversationHistory is { Count: > 0 })
        {
            messages.AddRange(conversationHistory.Select(m =>
                new AzureMessage { Role = m.Role, Content = m.Content }));
        }

        messages.Add(new AzureMessage { Role = "user", Content = userPrompt });

        return messages;
    }

    // ----- Internal DTOs -----

    private sealed class AzureChatRequest
    {
        public List<AzureMessage> Messages { get; set; } = [];
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
    }

    private sealed class AzureMessage
    {
        public string Role { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    private sealed class AzureChatResponse
    {
        public List<AzureChoice>? Choices { get; set; }
        public AzureUsage? Usage { get; set; }
    }

    private sealed class AzureChoice
    {
        public AzureMessage? Message { get; set; }
    }

    private sealed class AzureUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
    }

    private sealed class AzureEmbeddingRequest
    {
        public string Input { get; set; } = null!;
    }

    private sealed class AzureEmbeddingResponse
    {
        public List<AzureEmbeddingData>? Data { get; set; }
    }

    private sealed class AzureEmbeddingData
    {
        public float[] Embedding { get; set; } = [];
    }
}
