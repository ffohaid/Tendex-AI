using MediatR;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.Rag.Commands.IndexDocument;

/// <summary>
/// Command to index a document into the vector database for RAG retrieval.
/// Triggers the full indexing pipeline: download → extract → chunk → embed → store.
/// </summary>
public sealed record IndexDocumentCommand : IRequest<DocumentIndexingResult>
{
    /// <summary>The unique identifier of the document to index.</summary>
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

    /// <summary>Optional document category.</summary>
    public string? Category { get; init; }
}
