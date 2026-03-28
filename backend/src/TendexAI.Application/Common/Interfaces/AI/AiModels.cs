using TendexAI.Domain.Enums;

namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Represents a request to generate a text completion from an AI model.
/// </summary>
public sealed record AiCompletionRequest
{
    /// <summary>The tenant making the request.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>
    /// Optional: specific provider to use. If null, the gateway selects
    /// the highest-priority active provider for the tenant.
    /// </summary>
    public AiProvider? PreferredProvider { get; init; }

    /// <summary>Optional: specific model name override.</summary>
    public string? ModelNameOverride { get; init; }

    /// <summary>The system prompt that sets the AI behavior context.</summary>
    public string? SystemPrompt { get; init; }

    /// <summary>The user prompt / question.</summary>
    public required string UserPrompt { get; init; }

    /// <summary>Optional conversation history for multi-turn interactions.</summary>
    public IReadOnlyList<AiChatMessage>? ConversationHistory { get; init; }

    /// <summary>Optional: override max tokens for this specific request.</summary>
    public int? MaxTokensOverride { get; init; }

    /// <summary>Optional: override temperature for this specific request.</summary>
    public double? TemperatureOverride { get; init; }

    /// <summary>Optional: additional context from RAG retrieval.</summary>
    public string? RagContext { get; init; }
}

/// <summary>
/// Represents a single message in a conversation history.
/// </summary>
public sealed record AiChatMessage
{
    /// <summary>The role of the message sender (system, user, assistant).</summary>
    public required string Role { get; init; }

    /// <summary>The content of the message.</summary>
    public required string Content { get; init; }
}

/// <summary>
/// Represents the response from an AI completion request.
/// </summary>
public sealed record AiCompletionResponse
{
    /// <summary>The generated text content.</summary>
    public required string Content { get; init; }

    /// <summary>The AI provider that generated the response.</summary>
    public required AiProvider Provider { get; init; }

    /// <summary>The model name that generated the response.</summary>
    public required string ModelName { get; init; }

    /// <summary>Number of tokens used in the prompt.</summary>
    public int PromptTokens { get; init; }

    /// <summary>Number of tokens used in the completion.</summary>
    public int CompletionTokens { get; init; }

    /// <summary>Total tokens consumed.</summary>
    public int TotalTokens { get; init; }

    /// <summary>Time taken for the completion in milliseconds.</summary>
    public long LatencyMs { get; init; }

    /// <summary>Whether the response was successful.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>Error message if the request failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static AiCompletionResponse Success(
        string content,
        AiProvider provider,
        string modelName,
        int promptTokens = 0,
        int completionTokens = 0,
        long latencyMs = 0)
    {
        return new AiCompletionResponse
        {
            Content = content,
            Provider = provider,
            ModelName = modelName,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = promptTokens + completionTokens,
            LatencyMs = latencyMs,
            IsSuccess = true
        };
    }

    /// <summary>
    /// Creates a failure response.
    /// </summary>
    public static AiCompletionResponse Failure(
        string errorMessage,
        AiProvider provider,
        string modelName)
    {
        return new AiCompletionResponse
        {
            Content = string.Empty,
            Provider = provider,
            ModelName = modelName,
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Represents a request to generate embeddings from text.
/// </summary>
public sealed record AiEmbeddingRequest
{
    /// <summary>The tenant making the request.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>The text to generate embeddings for.</summary>
    public required string Text { get; init; }

    /// <summary>Optional: specific provider to use for embeddings.</summary>
    public AiProvider? PreferredProvider { get; init; }

    /// <summary>Optional: specific embedding model name.</summary>
    public string? ModelNameOverride { get; init; }
}

/// <summary>
/// Represents the response from an embedding generation request.
/// </summary>
public sealed record AiEmbeddingResponse
{
    /// <summary>The generated embedding vector.</summary>
    public required float[] Embedding { get; init; }

    /// <summary>The dimension of the embedding vector.</summary>
    public int Dimensions => Embedding.Length;

    /// <summary>The AI provider that generated the embedding.</summary>
    public required AiProvider Provider { get; init; }

    /// <summary>The model name that generated the embedding.</summary>
    public required string ModelName { get; init; }

    /// <summary>Whether the response was successful.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>Error message if the request failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful embedding response.
    /// </summary>
    public static AiEmbeddingResponse Success(
        float[] embedding,
        AiProvider provider,
        string modelName)
    {
        return new AiEmbeddingResponse
        {
            Embedding = embedding,
            Provider = provider,
            ModelName = modelName,
            IsSuccess = true
        };
    }

    /// <summary>
    /// Creates a failure embedding response.
    /// </summary>
    public static AiEmbeddingResponse Failure(
        string errorMessage,
        AiProvider provider,
        string modelName)
    {
        return new AiEmbeddingResponse
        {
            Embedding = [],
            Provider = provider,
            ModelName = modelName,
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
