namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Service for splitting documents into semantically meaningful chunks
/// for vector database indexing. Implements sentence-aware chunking strategy
/// as mandated by the RAG guidelines (fixed-size chunking is prohibited).
/// </summary>
public interface IDocumentChunkingService
{
    /// <summary>
    /// Splits a document's text content into overlapping chunks using
    /// sentence-aware boundaries. Each chunk includes a contextual header
    /// containing the document name, section, and page reference.
    /// </summary>
    /// <param name="request">The chunking request with document content and metadata.</param>
    /// <returns>A list of document chunks ready for embedding.</returns>
    IReadOnlyList<DocumentChunk> ChunkDocument(DocumentChunkingRequest request);
}

/// <summary>
/// Request parameters for document chunking.
/// </summary>
public sealed record DocumentChunkingRequest
{
    /// <summary>The full text content of the document.</summary>
    public required string Content { get; init; }

    /// <summary>The display name of the document.</summary>
    public required string DocumentName { get; init; }

    /// <summary>The unique identifier of the document.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Target chunk size in characters (approximate).
    /// Default: 1500 characters (~512-1024 tokens for Arabic text).
    /// </summary>
    public int TargetChunkSize { get; init; } = 1500;

    /// <summary>
    /// Overlap ratio between adjacent chunks (0.0 to 0.5).
    /// Default: 0.15 (15% overlap as per RAG guidelines).
    /// </summary>
    public double OverlapRatio { get; init; } = 0.15;

    /// <summary>Optional document category for metadata enrichment.</summary>
    public string? Category { get; init; }
}

/// <summary>
/// Represents a single chunk of a document after splitting.
/// </summary>
public sealed record DocumentChunk
{
    /// <summary>The text content of this chunk.</summary>
    public required string Text { get; init; }

    /// <summary>Zero-based index of this chunk within the document.</summary>
    public required int Index { get; init; }

    /// <summary>
    /// Contextual header for this chunk (document name + section + page).
    /// Prepended to the chunk text before embedding for improved retrieval.
    /// </summary>
    public required string ContextualHeader { get; init; }

    /// <summary>The section name this chunk belongs to, if detected.</summary>
    public string? SectionName { get; init; }

    /// <summary>The page number(s) this chunk originates from.</summary>
    public string? PageNumbers { get; init; }

    /// <summary>The start character position in the original document.</summary>
    public int StartPosition { get; init; }

    /// <summary>The end character position in the original document.</summary>
    public int EndPosition { get; init; }
}
