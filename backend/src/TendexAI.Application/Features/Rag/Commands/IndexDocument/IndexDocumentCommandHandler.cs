using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.Rag.Commands.IndexDocument;

/// <summary>
/// Handles the <see cref="IndexDocumentCommand"/> by delegating to the
/// <see cref="IDocumentIndexingService"/> which orchestrates the full
/// indexing pipeline (download → extract → chunk → embed → store).
/// </summary>
public sealed partial class IndexDocumentCommandHandler
    : IRequestHandler<IndexDocumentCommand, DocumentIndexingResult>
{
    private readonly IDocumentIndexingService _indexingService;
    private readonly ILogger<IndexDocumentCommandHandler> _logger;

    public IndexDocumentCommandHandler(
        IDocumentIndexingService indexingService,
        ILogger<IndexDocumentCommandHandler> logger)
    {
        _indexingService = indexingService;
        _logger = logger;
    }

    public async Task<DocumentIndexingResult> Handle(
        IndexDocumentCommand request,
        CancellationToken cancellationToken)
    {
        LogHandlingCommand(_logger, request.DocumentId, request.DocumentName);

        var indexingRequest = new DocumentIndexingRequest
        {
            DocumentId = request.DocumentId,
            ObjectKey = request.ObjectKey,
            ContentType = request.ContentType,
            DocumentName = request.DocumentName,
            CollectionName = request.CollectionName,
            TenantId = request.TenantId,
            Category = request.Category
        };

        return await _indexingService.IndexDocumentAsync(indexingRequest, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handling IndexDocumentCommand for {DocumentId} ({DocumentName})")]
    private static partial void LogHandlingCommand(ILogger logger, Guid documentId, string documentName);
}
