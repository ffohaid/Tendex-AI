namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Unified AI Gateway interface that abstracts communication with different AI providers.
/// Routes requests to the appropriate provider (OpenAI, Anthropic/Claude, local models)
/// based on tenant configuration and dynamic model switching.
/// </summary>
public interface IAiGateway
{
    /// <summary>
    /// Generates a text completion using the configured AI provider for the tenant.
    /// Automatically selects the highest-priority active provider unless overridden.
    /// </summary>
    /// <param name="request">The completion request containing prompt and configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI completion response.</returns>
    Task<AiCompletionResponse> GenerateCompletionAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for the given text using the configured embedding model.
    /// Used by the RAG engine for document indexing and semantic search.
    /// </summary>
    /// <param name="request">The embedding request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding response containing the vector.</returns>
    Task<AiEmbeddingResponse> GenerateEmbeddingAsync(
        AiEmbeddingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific AI provider is available and properly configured for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if at least one AI provider is configured and active.</returns>
    Task<bool> IsAvailableAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
