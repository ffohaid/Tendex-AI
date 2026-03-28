namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Abstraction for vector database operations (Qdrant).
/// Supports tenant-isolated document storage and hybrid search
/// for the Retrieval-Augmented Generation (RAG) engine.
/// </summary>
public interface IVectorStoreService
{
    /// <summary>
    /// Ensures the specified Qdrant collection exists with the correct schema.
    /// Creates the collection if it does not exist, including dense and sparse vector configurations.
    /// </summary>
    /// <param name="collectionName">The name of the Qdrant collection.</param>
    /// <param name="vectorSize">The dimensionality of the dense embedding vectors.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureCollectionExistsAsync(
        string collectionName,
        int vectorSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts (inserts or updates) a batch of document chunks with their embeddings
    /// into the specified Qdrant collection.
    /// </summary>
    /// <param name="collectionName">The target Qdrant collection.</param>
    /// <param name="points">The list of vector points to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpsertPointsAsync(
        string collectionName,
        IReadOnlyList<VectorPoint> points,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a semantic (dense vector) search in the specified collection.
    /// Results are filtered by tenant ID for multi-tenant isolation.
    /// </summary>
    /// <param name="request">The search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of search results ordered by relevance score.</returns>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorSearchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all points associated with a specific document from the collection.
    /// Used when a document is updated or removed from the knowledge base.
    /// </summary>
    /// <param name="collectionName">The target Qdrant collection.</param>
    /// <param name="documentId">The document identifier whose chunks should be removed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteByDocumentIdAsync(
        string collectionName,
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the Qdrant service is healthy and reachable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if Qdrant is healthy.</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a single vector point to be stored in Qdrant.
/// Each point corresponds to a document chunk with its embedding and metadata.
/// </summary>
public sealed record VectorPoint
{
    /// <summary>Unique identifier for this point.</summary>
    public required Guid Id { get; init; }

    /// <summary>The dense embedding vector for semantic search.</summary>
    public required float[] Vector { get; init; }

    /// <summary>Metadata payload stored alongside the vector.</summary>
    public required VectorPointPayload Payload { get; init; }
}

/// <summary>
/// Metadata payload for a vector point in Qdrant.
/// Includes document identification, chunk content, and contextual headers.
/// </summary>
public sealed record VectorPointPayload
{
    /// <summary>The tenant that owns this document.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>The source document identifier.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>The display name of the source document.</summary>
    public required string DocumentName { get; init; }

    /// <summary>The text content of this chunk.</summary>
    public required string ChunkText { get; init; }

    /// <summary>Zero-based index of this chunk within the document.</summary>
    public required int ChunkIndex { get; init; }

    /// <summary>
    /// Contextual header containing document name, section, and page number.
    /// Used to improve retrieval accuracy per RAG guidelines.
    /// </summary>
    public required string ContextualHeader { get; init; }

    /// <summary>The section or chapter name within the document.</summary>
    public string? SectionName { get; init; }

    /// <summary>The page number(s) this chunk originates from.</summary>
    public string? PageNumbers { get; init; }

    /// <summary>The document category (e.g., regulations, templates, proposals).</summary>
    public string? Category { get; init; }

    /// <summary>UTC timestamp when this chunk was indexed.</summary>
    public DateTime IndexedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Request parameters for vector similarity search.
/// </summary>
public sealed record VectorSearchRequest
{
    /// <summary>The target Qdrant collection to search.</summary>
    public required string CollectionName { get; init; }

    /// <summary>The query embedding vector.</summary>
    public required float[] QueryVector { get; init; }

    /// <summary>The tenant ID for multi-tenant filtering.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Maximum number of results to return.</summary>
    public int TopK { get; init; } = 20;

    /// <summary>Minimum similarity score threshold (0.0 to 1.0).</summary>
    public float ScoreThreshold { get; init; } = 0.5f;

    /// <summary>Optional: filter by specific document ID.</summary>
    public Guid? DocumentIdFilter { get; init; }

    /// <summary>Optional: filter by document category.</summary>
    public string? CategoryFilter { get; init; }
}

/// <summary>
/// Represents a single result from a vector similarity search.
/// </summary>
public sealed record VectorSearchResult
{
    /// <summary>The point identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>The similarity score (higher is more relevant).</summary>
    public required float Score { get; init; }

    /// <summary>The metadata payload of the matched point.</summary>
    public required VectorPointPayload Payload { get; init; }
}
