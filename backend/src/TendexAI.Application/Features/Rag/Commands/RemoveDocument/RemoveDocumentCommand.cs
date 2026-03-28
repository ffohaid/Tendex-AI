using MediatR;

namespace TendexAI.Application.Features.Rag.Commands.RemoveDocument;

/// <summary>
/// Command to remove all indexed vectors for a specific document from the vector database.
/// </summary>
public sealed record RemoveDocumentCommand : IRequest<bool>
{
    /// <summary>The Qdrant collection name.</summary>
    public required string CollectionName { get; init; }

    /// <summary>The document identifier whose vectors should be removed.</summary>
    public required Guid DocumentId { get; init; }
}
