using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Application.Common.IntegrationEvents;

/// <summary>
/// Integration event published when a document is approved and needs
/// to be indexed in the vector database (Qdrant) for RAG capabilities.
/// The AI service consumes this event to process and embed the document.
/// </summary>
public sealed record DocumentIndexRequestedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// The unique identifier of the document to be indexed.
    /// </summary>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// The MinIO object key for the document file.
    /// </summary>
    public required string ObjectKey { get; init; }

    /// <summary>
    /// The MIME type of the document (e.g., "application/pdf").
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// The display name of the document.
    /// </summary>
    public required string DocumentName { get; init; }

    /// <summary>
    /// The Qdrant collection name where the document should be indexed.
    /// </summary>
    public required string CollectionName { get; init; }

    /// <summary>
    /// Optional document category for metadata enrichment (e.g., regulations, templates, proposals).
    /// </summary>
    public string? Category { get; init; }
}
