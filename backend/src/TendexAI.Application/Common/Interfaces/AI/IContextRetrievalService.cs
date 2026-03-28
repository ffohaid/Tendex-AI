namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Service for retrieving relevant context from the vector database
/// for Retrieval-Augmented Generation (RAG). Implements hybrid search
/// combining semantic similarity with keyword matching, plus reranking.
/// </summary>
public interface IContextRetrievalService
{
    /// <summary>
    /// Retrieves the most relevant document chunks for a given query.
    /// Pipeline: Embed query → Semantic search → Rerank → Format context.
    /// </summary>
    /// <param name="request">The retrieval request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The retrieval result containing formatted context and source references.</returns>
    Task<ContextRetrievalResult> RetrieveContextAsync(
        ContextRetrievalRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request parameters for context retrieval.
/// </summary>
public sealed record ContextRetrievalRequest
{
    /// <summary>The user's query or question in natural language.</summary>
    public required string Query { get; init; }

    /// <summary>The tenant identifier for multi-tenant isolation.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>The Qdrant collection to search.</summary>
    public required string CollectionName { get; init; }

    /// <summary>
    /// Maximum number of context chunks to return after reranking.
    /// Default: 5 (as recommended by RAG guidelines).
    /// </summary>
    public int TopK { get; init; } = 5;

    /// <summary>
    /// Number of initial candidates to retrieve before reranking.
    /// Default: 20 (retrieve more, then rerank to top K).
    /// </summary>
    public int InitialCandidates { get; init; } = 20;

    /// <summary>Minimum similarity score threshold.</summary>
    public float ScoreThreshold { get; init; } = 0.5f;

    /// <summary>Optional: filter by specific document ID.</summary>
    public Guid? DocumentIdFilter { get; init; }

    /// <summary>Optional: filter by document category.</summary>
    public string? CategoryFilter { get; init; }
}

/// <summary>
/// Result of a context retrieval operation.
/// </summary>
public sealed record ContextRetrievalResult
{
    /// <summary>Whether the retrieval was successful.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// The formatted context string ready for injection into AI prompts.
    /// Wrapped in XML tags as per RAG guidelines:
    /// &lt;document&gt;&lt;source&gt;...&lt;/source&gt;&lt;document_content&gt;...&lt;/document_content&gt;&lt;/document&gt;
    /// </summary>
    public string? FormattedContext { get; init; }

    /// <summary>The individual context chunks with their metadata and scores.</summary>
    public IReadOnlyList<RetrievedChunk> Chunks { get; init; } = [];

    /// <summary>The total number of chunks retrieved before reranking.</summary>
    public int TotalCandidates { get; init; }

    /// <summary>Time taken for the retrieval operation in milliseconds.</summary>
    public long RetrievalTimeMs { get; init; }

    /// <summary>Error message if the retrieval failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Creates a successful retrieval result.</summary>
    public static ContextRetrievalResult Success(
        string formattedContext,
        IReadOnlyList<RetrievedChunk> chunks,
        int totalCandidates,
        long retrievalTimeMs)
    {
        return new ContextRetrievalResult
        {
            IsSuccess = true,
            FormattedContext = formattedContext,
            Chunks = chunks,
            TotalCandidates = totalCandidates,
            RetrievalTimeMs = retrievalTimeMs
        };
    }

    /// <summary>Creates a failed retrieval result.</summary>
    public static ContextRetrievalResult Failure(string errorMessage)
    {
        return new ContextRetrievalResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Represents a single retrieved chunk with its relevance score and metadata.
/// </summary>
public sealed record RetrievedChunk
{
    /// <summary>The text content of the chunk.</summary>
    public required string Text { get; init; }

    /// <summary>The relevance score (higher is better).</summary>
    public required float Score { get; init; }

    /// <summary>The source document name.</summary>
    public required string DocumentName { get; init; }

    /// <summary>The source document identifier.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>The section name within the document.</summary>
    public string? SectionName { get; init; }

    /// <summary>The page number(s) reference.</summary>
    public string? PageNumbers { get; init; }

    /// <summary>The contextual header of this chunk.</summary>
    public required string ContextualHeader { get; init; }

    /// <summary>The chunk index within the document.</summary>
    public int ChunkIndex { get; init; }
}
