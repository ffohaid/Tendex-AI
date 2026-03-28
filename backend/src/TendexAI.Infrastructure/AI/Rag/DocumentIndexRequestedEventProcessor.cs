using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Application.Common.IntegrationEvents;

namespace TendexAI.Infrastructure.AI.Rag;

/// <summary>
/// Processes <see cref="DocumentIndexRequestedIntegrationEvent"/> events
/// received from RabbitMQ. Triggers the document indexing pipeline to
/// vectorize and store document chunks in Qdrant for RAG retrieval.
///
/// This handler is idempotent: re-processing the same event will
/// re-index the document (replacing existing vectors).
/// </summary>
public sealed partial class DocumentIndexRequestedEventProcessor
    : IIntegrationEventProcessor<DocumentIndexRequestedIntegrationEvent>
{
    private readonly IDocumentIndexingService _indexingService;
    private readonly ILogger<DocumentIndexRequestedEventProcessor> _logger;

    public DocumentIndexRequestedEventProcessor(
        IDocumentIndexingService indexingService,
        ILogger<DocumentIndexRequestedEventProcessor> logger)
    {
        _indexingService = indexingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(
        DocumentIndexRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        LogEventReceived(_logger, integrationEvent.Id, integrationEvent.DocumentId, integrationEvent.DocumentName);

        try
        {
            var request = new DocumentIndexingRequest
            {
                DocumentId = integrationEvent.DocumentId,
                ObjectKey = integrationEvent.ObjectKey,
                ContentType = integrationEvent.ContentType,
                DocumentName = integrationEvent.DocumentName,
                CollectionName = integrationEvent.CollectionName,
                TenantId = integrationEvent.TenantId ?? Guid.Empty,
                Category = integrationEvent.Category
            };

            var result = await _indexingService.ReindexDocumentAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                LogIndexingSucceeded(_logger, integrationEvent.DocumentId,
                    result.ChunkCount, result.ProcessingTimeMs);
            }
            else
            {
                LogIndexingFailed(_logger, integrationEvent.DocumentId, result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            LogEventProcessingFailed(_logger, ex, integrationEvent.Id, integrationEvent.DocumentId);
            throw; // Re-throw to trigger RabbitMQ retry/DLQ mechanism
        }
    }

    // -------------------------------------------------------------------------
    // LoggerMessage delegates
    // -------------------------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Information, Message = "Received DocumentIndexRequested event {EventId} for document {DocumentId} ({DocumentName})")]
    private static partial void LogEventReceived(ILogger logger, Guid eventId, Guid documentId, string documentName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Document {DocumentId} indexed successfully: {ChunkCount} chunks in {ElapsedMs}ms")]
    private static partial void LogIndexingSucceeded(ILogger logger, Guid documentId, int chunkCount, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Document {DocumentId} indexing failed: {ErrorMessage}")]
    private static partial void LogIndexingFailed(ILogger logger, Guid documentId, string errorMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process DocumentIndexRequested event {EventId} for document {DocumentId}")]
    private static partial void LogEventProcessingFailed(ILogger logger, Exception ex, Guid eventId, Guid documentId);
}
