namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Orchestrates the full document indexing pipeline for RAG:
/// 1. Downloads document from MinIO
/// 2. Extracts text content
/// 3. Chunks the text using sentence-aware strategy
/// 4. Generates embeddings via AI Gateway
/// 5. Stores vectors in Qdrant with tenant isolation
/// </summary>
public interface IDocumentIndexingService
{
    /// <summary>
    /// Indexes a document into the vector database for RAG retrieval.
    /// This is the main entry point for the indexing pipeline.
    /// </summary>
    /// <param name="request">The indexing request containing document metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The indexing result with statistics.</returns>
    Task<DocumentIndexingResult> IndexDocumentAsync(
        DocumentIndexingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-indexes a document by first removing existing vectors and then
    /// performing a fresh indexing. Used when document content is updated.
    /// </summary>
    /// <param name="request">The indexing request containing document metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The indexing result with statistics.</returns>
    Task<DocumentIndexingResult> ReindexDocumentAsync(
        DocumentIndexingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all indexed vectors for a specific document from the vector database.
    /// </summary>
    /// <param name="collectionName">The Qdrant collection name.</param>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveDocumentAsync(
        string collectionName,
        Guid documentId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request parameters for document indexing.
/// </summary>
public sealed record DocumentIndexingRequest
{
    /// <summary>The unique identifier of the document.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>The MinIO object key for the document file.</summary>
    public required string ObjectKey { get; init; }

    /// <summary>The MIME type of the document.</summary>
    public required string ContentType { get; init; }

    /// <summary>The display name of the document.</summary>
    public required string DocumentName { get; init; }

    /// <summary>The Qdrant collection name for storage.</summary>
    public required string CollectionName { get; init; }

    /// <summary>The tenant that owns this document.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Optional document category for metadata enrichment.</summary>
    public string? Category { get; init; }
}

/// <summary>
/// Result of a document indexing operation.
/// </summary>
public sealed record DocumentIndexingResult
{
    /// <summary>Whether the indexing was successful.</summary>
    public required bool IsSuccess { get; init; }

    /// <summary>The document identifier that was indexed.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>The number of chunks created from the document.</summary>
    public int ChunkCount { get; init; }

    /// <summary>The total number of characters in the document.</summary>
    public int TotalCharacters { get; init; }

    /// <summary>The embedding model used for vectorization.</summary>
    public string? EmbeddingModel { get; init; }

    /// <summary>The vector dimensionality of the embeddings.</summary>
    public int VectorDimensions { get; init; }

    /// <summary>Time taken for the indexing operation in milliseconds.</summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>Error message if the indexing failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Creates a successful indexing result.</summary>
    public static DocumentIndexingResult Success(
        Guid documentId,
        int chunkCount,
        int totalCharacters,
        string embeddingModel,
        int vectorDimensions,
        long processingTimeMs)
    {
        return new DocumentIndexingResult
        {
            IsSuccess = true,
            DocumentId = documentId,
            ChunkCount = chunkCount,
            TotalCharacters = totalCharacters,
            EmbeddingModel = embeddingModel,
            VectorDimensions = vectorDimensions,
            ProcessingTimeMs = processingTimeMs
        };
    }

    /// <summary>Creates a failed indexing result.</summary>
    public static DocumentIndexingResult Failure(Guid documentId, string errorMessage)
    {
        return new DocumentIndexingResult
        {
            IsSuccess = false,
            DocumentId = documentId,
            ErrorMessage = errorMessage
        };
    }
}
