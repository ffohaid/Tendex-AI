using MediatR;

namespace TendexAI.Application.Features.Rag.Queries.GetVectorStoreStatus;

/// <summary>
/// Query to check the health status of the vector store (Qdrant).
/// </summary>
public sealed record GetVectorStoreStatusQuery : IRequest<VectorStoreStatusDto>;

/// <summary>
/// DTO representing the vector store health status.
/// </summary>
public sealed record VectorStoreStatusDto
{
    /// <summary>Whether the vector store is healthy and reachable.</summary>
    public required bool IsHealthy { get; init; }

    /// <summary>The vector store service name.</summary>
    public string Service { get; init; } = "Qdrant";

    /// <summary>UTC timestamp of the status check.</summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}
