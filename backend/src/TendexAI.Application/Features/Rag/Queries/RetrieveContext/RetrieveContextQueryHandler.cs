using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.Rag.Queries.RetrieveContext;

/// <summary>
/// Handles the <see cref="RetrieveContextQuery"/> by delegating to the
/// <see cref="IContextRetrievalService"/> for semantic search and reranking.
/// </summary>
public sealed partial class RetrieveContextQueryHandler
    : IRequestHandler<RetrieveContextQuery, ContextRetrievalResult>
{
    private readonly IContextRetrievalService _retrievalService;
    private readonly ILogger<RetrieveContextQueryHandler> _logger;

    public RetrieveContextQueryHandler(
        IContextRetrievalService retrievalService,
        ILogger<RetrieveContextQueryHandler> logger)
    {
        _retrievalService = retrievalService;
        _logger = logger;
    }

    public async Task<ContextRetrievalResult> Handle(
        RetrieveContextQuery request,
        CancellationToken cancellationToken)
    {
        LogHandlingQuery(_logger, request.Query, request.CollectionName);

        return await _retrievalService.RetrieveContextAsync(
            new ContextRetrievalRequest
            {
                Query = request.Query,
                TenantId = request.TenantId,
                CollectionName = request.CollectionName,
                TopK = request.TopK,
                ScoreThreshold = request.ScoreThreshold,
                DocumentIdFilter = request.DocumentIdFilter,
                CategoryFilter = request.CategoryFilter
            },
            cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handling RetrieveContextQuery for '{Query}' in collection '{CollectionName}'")]
    private static partial void LogHandlingQuery(ILogger logger, string query, string collectionName);
}
