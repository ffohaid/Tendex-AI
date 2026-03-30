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
/// AI provider client for Google Gemini (Generative Language API).
/// Uses the Gemini REST API with API key authentication.
/// Endpoint format: https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}
/// </summary>
public sealed class GoogleGeminiProviderClient : IAiProviderClient
{
    private const string DefaultGeminiEndpoint = "https://generativelanguage.googleapis.com/v1beta";
    private const string DefaultModel = "gemini-2.5-flash";
    private const string DefaultEmbeddingModel = "text-embedding-004";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleGeminiProviderClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public GoogleGeminiProviderClient(
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleGeminiProviderClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public AiProvider Provider => AiProvider.GoogleVertexAI;

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
        var baseUrl = endpoint ?? DefaultGeminiEndpoint;
        var model = string.IsNullOrWhiteSpace(modelName) ? DefaultModel : modelName;
        var sw = Stopwatch.StartNew();

        try
        {
            var request = BuildGeminiRequest(systemPrompt, userPrompt, conversationHistory, maxTokens, temperature);
            var json = JsonSerializer.Serialize(request, JsonOptions);

            using var client = _httpClientFactory.CreateClient("GoogleGemini");
            var requestUrl = $"{baseUrl.TrimEnd('/')}/models/{model}:generateContent?key={apiKey}";

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(requestUrl, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Gemini API returned {StatusCode} for model {Model}: {Body}",
                    response.StatusCode, model, responseBody);

                return AiCompletionResponse.Failure(
                    $"Gemini API error: {response.StatusCode} - {responseBody}",
                    AiProvider.GoogleVertexAI,
                    model);
            }

            var result = JsonSerializer.Deserialize<GeminiResponse>(responseBody, JsonOptions);
            var textContent = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(textContent))
            {
                _logger.LogWarning("Gemini returned empty content for model {Model}", model);
                return AiCompletionResponse.Failure(
                    "Gemini returned empty content.",
                    AiProvider.GoogleVertexAI,
                    model);
            }

            var promptTokens = result?.UsageMetadata?.PromptTokenCount ?? 0;
            var completionTokens = result?.UsageMetadata?.CandidatesTokenCount ?? 0;

            return AiCompletionResponse.Success(
                content: textContent,
                provider: AiProvider.GoogleVertexAI,
                modelName: model,
                promptTokens: promptTokens,
                completionTokens: completionTokens,
                latencyMs: sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API for model {Model}", model);
            return AiCompletionResponse.Failure(
                $"Gemini API exception: {ex.Message}",
                AiProvider.GoogleVertexAI,
                model);
        }
    }

    // Known Gemini embedding models that support embedContent API
    private static readonly HashSet<string> s_knownEmbeddingModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "text-embedding-004",
        "text-embedding-005",
        "embedding-001",
        "text-multilingual-embedding-002"
    };

    /// <summary>
    /// Determines the correct embedding model to use.
    /// If the caller passes a completion model (e.g., gemini-2.5-flash) instead of
    /// an embedding model, we fall back to the DefaultEmbeddingModel.
    /// </summary>
    private static string ResolveEmbeddingModel(string? modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            return DefaultEmbeddingModel;

        // If the model is a known embedding model, use it directly
        if (s_knownEmbeddingModels.Contains(modelName))
            return modelName;

        // If the model contains "embedding" in its name, assume it's valid
        if (modelName.Contains("embedding", StringComparison.OrdinalIgnoreCase))
            return modelName;

        // Otherwise, the caller likely passed a completion model — use default embedding model
        return DefaultEmbeddingModel;
    }

    /// <inheritdoc />
    public async Task<AiEmbeddingResponse> SendEmbeddingAsync(
        string apiKey,
        string? endpoint,
        string modelName,
        string text,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = endpoint ?? DefaultGeminiEndpoint;
        var embeddingModel = ResolveEmbeddingModel(modelName);

        try
        {
            var request = new GeminiEmbeddingRequest
            {
                Model = $"models/{embeddingModel}",
                Content = new GeminiContent
                {
                    Parts = [new GeminiPart { Text = text }]
                }
            };

            var json = JsonSerializer.Serialize(request, JsonOptions);

            using var client = _httpClientFactory.CreateClient("GoogleGemini");
            var requestUrl = $"{baseUrl.TrimEnd('/')}/models/{embeddingModel}:embedContent?key={apiKey}";

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(requestUrl, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Gemini Embedding API returned {StatusCode}: {Body}",
                    response.StatusCode, responseBody);

                return AiEmbeddingResponse.Failure(
                    $"Gemini Embedding API error: {response.StatusCode}",
                    AiProvider.GoogleVertexAI,
                    embeddingModel);
            }

            var result = JsonSerializer.Deserialize<GeminiEmbeddingResponse>(responseBody, JsonOptions);
            var embedding = result?.Embedding?.Values ?? [];

            return AiEmbeddingResponse.Success(
                embedding: embedding,
                provider: AiProvider.GoogleVertexAI,
                modelName: embeddingModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini Embedding API for model {Model}", embeddingModel);
            return AiEmbeddingResponse.Failure(
                $"Gemini Embedding API exception: {ex.Message}",
                AiProvider.GoogleVertexAI,
                embeddingModel);
        }
    }

    // ----- Helper Methods -----

    private static GeminiRequest BuildGeminiRequest(
        string? systemPrompt,
        string userPrompt,
        IReadOnlyList<AiChatMessage>? conversationHistory,
        int maxTokens,
        double temperature)
    {
        var request = new GeminiRequest
        {
            GenerationConfig = new GeminiGenerationConfig
            {
                MaxOutputTokens = maxTokens,
                Temperature = temperature
            }
        };

        // Add system instruction if provided
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            request.SystemInstruction = new GeminiContent
            {
                Parts = [new GeminiPart { Text = systemPrompt }]
            };
        }

        // Build contents array
        var contents = new List<GeminiContent>();

        // Add conversation history
        if (conversationHistory is { Count: > 0 })
        {
            foreach (var msg in conversationHistory)
            {
                var role = msg.Role?.ToLowerInvariant() switch
                {
                    "assistant" => "model",
                    "system" => "user", // Gemini doesn't support system in contents
                    _ => "user"
                };

                contents.Add(new GeminiContent
                {
                    Role = role,
                    Parts = [new GeminiPart { Text = msg.Content }]
                });
            }
        }

        // Add the current user message
        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = [new GeminiPart { Text = userPrompt }]
        });

        request.Contents = contents;
        return request;
    }

    // ----- Internal DTOs for Gemini API -----

    private sealed class GeminiRequest
    {
        public List<GeminiContent>? Contents { get; set; }
        public GeminiContent? SystemInstruction { get; set; }
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private sealed class GeminiContent
    {
        public string? Role { get; set; }
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        public string Text { get; set; } = null!;
    }

    private sealed class GeminiGenerationConfig
    {
        public int MaxOutputTokens { get; set; }
        public double Temperature { get; set; }
    }

    private sealed class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
        public GeminiUsageMetadata? UsageMetadata { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    private sealed class GeminiUsageMetadata
    {
        public int PromptTokenCount { get; set; }
        public int CandidatesTokenCount { get; set; }
        public int TotalTokenCount { get; set; }
    }

    private sealed class GeminiEmbeddingRequest
    {
        public string Model { get; set; } = null!;
        public GeminiContent Content { get; set; } = null!;
    }

    private sealed class GeminiEmbeddingResponse
    {
        public GeminiEmbeddingValues? Embedding { get; set; }
    }

    private sealed class GeminiEmbeddingValues
    {
        public float[] Values { get; set; } = [];
    }
}
