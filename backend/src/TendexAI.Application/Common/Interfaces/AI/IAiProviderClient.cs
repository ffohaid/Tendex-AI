using TendexAI.Domain.Enums;

namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Interface for individual AI provider clients (OpenAI, Anthropic, Local models).
/// Each provider implements this interface to handle provider-specific API communication.
/// </summary>
public interface IAiProviderClient
{
    /// <summary>
    /// The AI provider type this client handles.
    /// </summary>
    AiProvider Provider { get; }

    /// <summary>
    /// Sends a chat completion request to the AI provider.
    /// </summary>
    /// <param name="apiKey">The decrypted API key (in-memory only).</param>
    /// <param name="endpoint">Optional custom endpoint URL.</param>
    /// <param name="modelName">The model name to use.</param>
    /// <param name="systemPrompt">The system prompt.</param>
    /// <param name="userPrompt">The user prompt.</param>
    /// <param name="conversationHistory">Optional conversation history.</param>
    /// <param name="maxTokens">Maximum tokens for the response.</param>
    /// <param name="temperature">Temperature for response randomness.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completion response from the provider.</returns>
    Task<AiCompletionResponse> SendCompletionAsync(
        string apiKey,
        string? endpoint,
        string modelName,
        string? systemPrompt,
        string userPrompt,
        IReadOnlyList<AiChatMessage>? conversationHistory,
        int maxTokens,
        double temperature,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an embedding generation request to the AI provider.
    /// </summary>
    /// <param name="apiKey">The decrypted API key (in-memory only).</param>
    /// <param name="endpoint">Optional custom endpoint URL.</param>
    /// <param name="modelName">The embedding model name.</param>
    /// <param name="text">The text to generate embeddings for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding response from the provider.</returns>
    Task<AiEmbeddingResponse> SendEmbeddingAsync(
        string apiKey,
        string? endpoint,
        string modelName,
        string text,
        CancellationToken cancellationToken = default);
}
