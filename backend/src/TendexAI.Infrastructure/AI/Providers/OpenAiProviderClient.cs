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
/// AI provider client for OpenAI and Azure OpenAI APIs.
/// Handles chat completions and embedding generation via the OpenAI-compatible API.
/// </summary>
public sealed class OpenAiProviderClient : IAiProviderClient
{
    private const string DefaultOpenAiEndpoint = "https://api.openai.com/v1";
    private const string DefaultEmbeddingModel = "text-embedding-3-small";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiProviderClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OpenAiProviderClient(
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAiProviderClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public AiProvider Provider => AiProvider.OpenAI;

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
        var sw = Stopwatch.StartNew();
        var baseUrl = endpoint ?? DefaultOpenAiEndpoint;

        try
        {
            var messages = BuildMessages(systemPrompt, userPrompt, conversationHistory);

            var requestBody = new OpenAiChatRequest
            {
                Model = modelName,
                Messages = messages,
                MaxTokens = maxTokens,
                Temperature = temperature
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);

            using var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestUrl = $"{baseUrl.TrimEnd('/')}/chat/completions";

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(requestUrl, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenAI API returned {StatusCode} for model {Model}: {Body}",
                    response.StatusCode, modelName, responseBody);

                return AiCompletionResponse.Failure(
                    $"OpenAI API error: {response.StatusCode} - {responseBody}",
                    AiProvider.OpenAI,
                    modelName);
            }

            var result = JsonSerializer.Deserialize<OpenAiChatResponse>(responseBody, JsonOptions);

            sw.Stop();

            var completionContent = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

            return AiCompletionResponse.Success(
                content: completionContent,
                provider: AiProvider.OpenAI,
                modelName: modelName,
                promptTokens: result?.Usage?.PromptTokens ?? 0,
                completionTokens: result?.Usage?.CompletionTokens ?? 0,
                latencyMs: sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API for model {Model}", modelName);
            return AiCompletionResponse.Failure(
                $"OpenAI API exception: {ex.Message}",
                AiProvider.OpenAI,
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
        var baseUrl = endpoint ?? DefaultOpenAiEndpoint;
        var embeddingModel = string.IsNullOrWhiteSpace(modelName) ? DefaultEmbeddingModel : modelName;

        try
        {
            var requestBody = new OpenAiEmbeddingRequest
            {
                Model = embeddingModel,
                Input = text
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);

            using var client = _httpClientFactory.CreateClient("OpenAI");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestUrl = $"{baseUrl.TrimEnd('/')}/embeddings";

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(requestUrl, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenAI Embedding API returned {StatusCode}: {Body}",
                    response.StatusCode, responseBody);

                return AiEmbeddingResponse.Failure(
                    $"OpenAI Embedding API error: {response.StatusCode}",
                    AiProvider.OpenAI,
                    embeddingModel);
            }

            var result = JsonSerializer.Deserialize<OpenAiEmbeddingResponse>(responseBody, JsonOptions);
            var embedding = result?.Data?.FirstOrDefault()?.Embedding ?? [];

            return AiEmbeddingResponse.Success(
                embedding: embedding,
                provider: AiProvider.OpenAI,
                modelName: embeddingModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI Embedding API for model {Model}", embeddingModel);
            return AiEmbeddingResponse.Failure(
                $"OpenAI Embedding API exception: {ex.Message}",
                AiProvider.OpenAI,
                embeddingModel);
        }
    }

    // ----- Helper Methods -----

    private static List<OpenAiMessage> BuildMessages(
        string? systemPrompt,
        string userPrompt,
        IReadOnlyList<AiChatMessage>? conversationHistory)
    {
        var messages = new List<OpenAiMessage>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new OpenAiMessage { Role = "system", Content = systemPrompt });
        }

        if (conversationHistory is { Count: > 0 })
        {
            messages.AddRange(conversationHistory.Select(m =>
                new OpenAiMessage { Role = m.Role, Content = m.Content }));
        }

        messages.Add(new OpenAiMessage { Role = "user", Content = userPrompt });

        return messages;
    }

    // ----- Internal DTOs for OpenAI API -----

    private sealed class OpenAiChatRequest
    {
        public string Model { get; set; } = null!;
        public List<OpenAiMessage> Messages { get; set; } = [];
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
    }

    private sealed class OpenAiMessage
    {
        public string Role { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    private sealed class OpenAiChatResponse
    {
        public List<OpenAiChoice>? Choices { get; set; }
        public OpenAiUsage? Usage { get; set; }
    }

    private sealed class OpenAiChoice
    {
        public OpenAiMessage? Message { get; set; }
    }

    private sealed class OpenAiUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    private sealed class OpenAiEmbeddingRequest
    {
        public string Model { get; set; } = null!;
        public string Input { get; set; } = null!;
    }

    private sealed class OpenAiEmbeddingResponse
    {
        public List<OpenAiEmbeddingData>? Data { get; set; }
    }

    private sealed class OpenAiEmbeddingData
    {
        public float[] Embedding { get; set; } = [];
    }
}
