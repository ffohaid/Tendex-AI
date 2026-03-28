using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.AI.Providers;

/// <summary>
/// AI provider client for Anthropic Claude API.
/// Handles chat completions via the Anthropic Messages API.
/// </summary>
public sealed class AnthropicProviderClient : IAiProviderClient
{
    private const string DefaultAnthropicEndpoint = "https://api.anthropic.com/v1";
    private const string AnthropicApiVersion = "2023-06-01";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnthropicProviderClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AnthropicProviderClient(
        IHttpClientFactory httpClientFactory,
        ILogger<AnthropicProviderClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public AiProvider Provider => AiProvider.Anthropic;

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
        var baseUrl = endpoint ?? DefaultAnthropicEndpoint;

        try
        {
            var messages = BuildMessages(userPrompt, conversationHistory);

            var requestBody = new AnthropicMessagesRequest
            {
                Model = modelName,
                System = systemPrompt,
                Messages = messages,
                MaxTokens = maxTokens,
                Temperature = temperature
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);

            using var client = _httpClientFactory.CreateClient("Anthropic");
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", AnthropicApiVersion);

            var requestUrl = $"{baseUrl.TrimEnd('/')}/messages";

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(requestUrl, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Anthropic API returned {StatusCode} for model {Model}: {Body}",
                    response.StatusCode, modelName, responseBody);

                return AiCompletionResponse.Failure(
                    $"Anthropic API error: {response.StatusCode} - {responseBody}",
                    AiProvider.Anthropic,
                    modelName);
            }

            var result = JsonSerializer.Deserialize<AnthropicMessagesResponse>(responseBody, JsonOptions);

            sw.Stop();

            var completionContent = result?.Content?.FirstOrDefault()?.Text ?? string.Empty;

            return AiCompletionResponse.Success(
                content: completionContent,
                provider: AiProvider.Anthropic,
                modelName: modelName,
                promptTokens: result?.Usage?.InputTokens ?? 0,
                completionTokens: result?.Usage?.OutputTokens ?? 0,
                latencyMs: sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Anthropic API for model {Model}", modelName);
            return AiCompletionResponse.Failure(
                $"Anthropic API exception: {ex.Message}",
                AiProvider.Anthropic,
                modelName);
        }
    }

    /// <inheritdoc />
    public Task<AiEmbeddingResponse> SendEmbeddingAsync(
        string apiKey,
        string? endpoint,
        string modelName,
        string text,
        CancellationToken cancellationToken = default)
    {
        // Anthropic does not provide a native embedding API.
        // Embedding requests for Anthropic tenants should be routed to OpenAI or a local model.
        _logger.LogWarning(
            "Anthropic does not support embedding generation. " +
            "Use OpenAI or a local model for embeddings.");

        return Task.FromResult(AiEmbeddingResponse.Failure(
            "Anthropic does not support embedding generation. Use OpenAI or a local model.",
            AiProvider.Anthropic,
            modelName));
    }

    // ----- Helper Methods -----

    private static List<AnthropicMessage> BuildMessages(
        string userPrompt,
        IReadOnlyList<AiChatMessage>? conversationHistory)
    {
        var messages = new List<AnthropicMessage>();

        if (conversationHistory is { Count: > 0 })
        {
            messages.AddRange(conversationHistory
                .Where(m => m.Role != "system") // System prompt is handled separately in Anthropic API
                .Select(m => new AnthropicMessage
                {
                    Role = m.Role,
                    Content = m.Content
                }));
        }

        messages.Add(new AnthropicMessage { Role = "user", Content = userPrompt });

        return messages;
    }

    // ----- Internal DTOs for Anthropic API -----

    private sealed class AnthropicMessagesRequest
    {
        public string Model { get; set; } = null!;
        public string? System { get; set; }
        public List<AnthropicMessage> Messages { get; set; } = [];
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
    }

    private sealed class AnthropicMessage
    {
        public string Role { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    private sealed class AnthropicMessagesResponse
    {
        public List<AnthropicContentBlock>? Content { get; set; }
        public AnthropicUsage? Usage { get; set; }
    }

    private sealed class AnthropicContentBlock
    {
        public string Type { get; set; } = null!;
        public string Text { get; set; } = null!;
    }

    private sealed class AnthropicUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }
        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }
}
