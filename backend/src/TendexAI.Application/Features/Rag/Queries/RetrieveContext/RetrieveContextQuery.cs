using MediatR;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.Rag.Queries.RetrieveContext;

/// <summary>
/// Query to retrieve relevant context from the vector database for RAG.
/// </summary>
public sealed record RetrieveContextQuery : IRequest<ContextRetrievalResult>
{
    /// <summary>The user's query or question.</summary>
    public required string Query { get; init; }

    /// <summary>The tenant identifier.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>The Qdrant collection to search.</summary>
    public required string CollectionName { get; init; }

    /// <summary>Maximum number of context chunks to return.</summary>
    public int TopK { get; init; } = 5;

    /// <summary>Minimum similarity score threshold.</summary>
    public float ScoreThreshold { get; init; } = 0.5f;

    /// <summary>Optional: filter by specific document ID.</summary>
    public Guid? DocumentIdFilter { get; init; }

    /// <summary>Optional: filter by document category.</summary>
    public string? CategoryFilter { get; init; }
}
