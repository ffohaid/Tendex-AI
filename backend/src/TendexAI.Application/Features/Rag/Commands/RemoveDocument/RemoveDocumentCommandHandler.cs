using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.Rag.Commands.RemoveDocument;

/// <summary>
/// Handles the <see cref="RemoveDocumentCommand"/> by removing all indexed
/// vectors for the specified document from the vector database.
/// </summary>
public sealed partial class RemoveDocumentCommandHandler
    : IRequestHandler<RemoveDocumentCommand, bool>
{
    private readonly IDocumentIndexingService _indexingService;
    private readonly ILogger<RemoveDocumentCommandHandler> _logger;

    public RemoveDocumentCommandHandler(
        IDocumentIndexingService indexingService,
        ILogger<RemoveDocumentCommandHandler> logger)
    {
        _indexingService = indexingService;
        _logger = logger;
    }

    public async Task<bool> Handle(
        RemoveDocumentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            LogHandlingCommand(_logger, request.DocumentId, request.CollectionName);

            await _indexingService.RemoveDocumentAsync(
                request.CollectionName, request.DocumentId, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            LogRemovalFailed(_logger, ex, request.DocumentId, request.CollectionName);
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handling RemoveDocumentCommand for {DocumentId} in collection '{CollectionName}'")]
    private static partial void LogHandlingCommand(ILogger logger, Guid documentId, string collectionName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to remove document {DocumentId} from collection '{CollectionName}'")]
    private static partial void LogRemovalFailed(ILogger logger, Exception ex, Guid documentId, string collectionName);
}
