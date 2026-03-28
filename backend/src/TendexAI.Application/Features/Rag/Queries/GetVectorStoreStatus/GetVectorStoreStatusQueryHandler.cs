using MediatR;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.Rag.Queries.GetVectorStoreStatus;

/// <summary>
/// Handles the <see cref="GetVectorStoreStatusQuery"/> by checking
/// the health of the Qdrant vector store.
/// </summary>
public sealed class GetVectorStoreStatusQueryHandler
    : IRequestHandler<GetVectorStoreStatusQuery, VectorStoreStatusDto>
{
    private readonly IVectorStoreService _vectorStore;

    public GetVectorStoreStatusQueryHandler(IVectorStoreService vectorStore)
    {
        _vectorStore = vectorStore;
    }

    public async Task<VectorStoreStatusDto> Handle(
        GetVectorStoreStatusQuery request,
        CancellationToken cancellationToken)
    {
        var isHealthy = await _vectorStore.IsHealthyAsync(cancellationToken);

        return new VectorStoreStatusDto
        {
            IsHealthy = isHealthy,
            CheckedAt = DateTime.UtcNow
        };
    }
}
