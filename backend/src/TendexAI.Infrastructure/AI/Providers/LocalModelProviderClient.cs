using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.AI.Providers;

/// <summary>
/// AI provider client for local/on-premise models.
/// Supports any model that exposes an OpenAI-compatible API (e.g., Ollama, vLLM, LM Studio, LocalAI).
/// This enables the Dual Deployment Strategy: cloud + on-premise models.
/// </summary>
public sealed class LocalModelProviderClient : IAiProviderClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LocalModelProviderClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LocalModelProviderClient(
        IHttpClientFactory httpClientFactory,
        ILogger<LocalModelProviderClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public AiProvider Provider => AiProvider.Custom;

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
                "Local model endpoint is required but not configured.",
                AiProvider.Custom,
                modelName);
        }

        var sw = Stopwatch.StartNew();

        try
        {
            // Build messages in OpenAI-compatible format (most local model servers support this)
            var messages = BuildMessages(systemPrompt, userPrompt, conversationHistory);

            var requestBody = new LocalChatRequest
            {
                Model = modelName,
                Messages = messages,
                MaxTokens = maxTokens,
                Temperature = temperature
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);

            using var client = _httpClientFactory.CreateClient("LocalModel");

            // Some local models require an API key, some don't
            if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "none")
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            var requestUrl = $"{endpoint.TrimEnd('/')}/chat/completions";

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(requestUrl, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Local model API returned {StatusCode} for model {Model} at {Endpoint}: {Body}",
                    response.StatusCode, modelName, endpoint, responseBody);

                return AiCompletionResponse.Failure(
                    $"Local model API error: {response.StatusCode} - {responseBody}",
                    AiProvider.Custom,
                    modelName);
            }

            // Parse OpenAI-compatible response format
            var result = JsonSerializer.Deserialize<LocalChatResponse>(responseBody, JsonOptions);

            sw.Stop();

            var completionContent = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

            return AiCompletionResponse.Success(
                content: completionContent,
                provider: AiProvider.Custom,
                modelName: modelName,
                promptTokens: result?.Usage?.PromptTokens ?? 0,
                completionTokens: result?.Usage?.CompletionTokens ?? 0,
                latencyMs: sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error calling local model API for model {Model} at {Endpoint}",
                modelName, endpoint);

            return AiCompletionResponse.Failure(
                $"Local model API exception: {ex.Message}",
                AiProvider.Custom,
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
                "Local model endpoint is required but not configured.",
                AiProvider.Custom,
                modelName);
        }

        try
        {
            var requestBody = new LocalEmbeddingRequest
            {
                Model = modelName,
                Input = text
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);

            using var client = _httpClientFactory.CreateClient("LocalModel");

            if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "none")
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            var requestUrl = $"{endpoint.TrimEnd('/')}/embeddings";

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(requestUrl, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Local model Embedding API returned {StatusCode} at {Endpoint}: {Body}",
                    response.StatusCode, endpoint, responseBody);

                return AiEmbeddingResponse.Failure(
                    $"Local model Embedding API error: {response.StatusCode}",
                    AiProvider.Custom,
                    modelName);
            }

            var result = JsonSerializer.Deserialize<LocalEmbeddingResponse>(responseBody, JsonOptions);
            var embedding = result?.Data?.FirstOrDefault()?.Embedding ?? [];

            return AiEmbeddingResponse.Success(
                embedding: embedding,
                provider: AiProvider.Custom,
                modelName: modelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error calling local model Embedding API for model {Model} at {Endpoint}",
                modelName, endpoint);

            return AiEmbeddingResponse.Failure(
                $"Local model Embedding API exception: {ex.Message}",
                AiProvider.Custom,
                modelName);
        }
    }

    // ----- Helper Methods -----

    private static List<LocalMessage> BuildMessages(
        string? systemPrompt,
        string userPrompt,
        IReadOnlyList<AiChatMessage>? conversationHistory)
    {
        var messages = new List<LocalMessage>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new LocalMessage { Role = "system", Content = systemPrompt });
        }

        if (conversationHistory is { Count: > 0 })
        {
            messages.AddRange(conversationHistory.Select(m =>
                new LocalMessage { Role = m.Role, Content = m.Content }));
        }

        messages.Add(new LocalMessage { Role = "user", Content = userPrompt });

        return messages;
    }

    // ----- Internal DTOs (OpenAI-compatible format) -----

    private sealed class LocalChatRequest
    {
        public string Model { get; set; } = null!;
        public List<LocalMessage> Messages { get; set; } = [];
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
    }

    private sealed class LocalMessage
    {
        public string Role { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    private sealed class LocalChatResponse
    {
        public List<LocalChoice>? Choices { get; set; }
        public LocalUsage? Usage { get; set; }
    }

    private sealed class LocalChoice
    {
        public LocalMessage? Message { get; set; }
    }

    private sealed class LocalUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
    }

    private sealed class LocalEmbeddingRequest
    {
        public string Model { get; set; } = null!;
        public string Input { get; set; } = null!;
    }

    private sealed class LocalEmbeddingResponse
    {
        public List<LocalEmbeddingData>? Data { get; set; }
    }

    private sealed class LocalEmbeddingData
    {
        public float[] Embedding { get; set; } = [];
    }
}
