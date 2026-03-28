using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Infrastructure.AI.Rag;

/// <summary>
/// Orchestrates the full document indexing pipeline for RAG:
/// 1. Downloads document from MinIO object storage
/// 2. Extracts text content from the document
/// 3. Chunks the text using sentence-aware strategy
/// 4. Generates embeddings via the AI Gateway
/// 5. Stores vectors in Qdrant with tenant isolation and contextual metadata
///
/// Implements idempotent indexing — re-indexing the same document
/// replaces all existing vectors for that document.
/// </summary>
public sealed partial class DocumentIndexingService : IDocumentIndexingService
{
    private readonly IFileStorageService _fileStorage;
    private readonly IDocumentChunkingService _chunkingService;
    private readonly IAiGateway _aiGateway;
    private readonly IVectorStoreService _vectorStore;
    private readonly DocumentTextExtractor _textExtractor;
    private readonly ILogger<DocumentIndexingService> _logger;

    /// <summary>
    /// Maximum number of chunks to embed in a single batch.
    /// Prevents exceeding API rate limits and memory constraints.
    /// </summary>
    private const int EmbeddingBatchSize = 20;

    public DocumentIndexingService(
        IFileStorageService fileStorage,
        IDocumentChunkingService chunkingService,
        IAiGateway aiGateway,
        IVectorStoreService vectorStore,
        DocumentTextExtractor textExtractor,
        ILogger<DocumentIndexingService> logger)
    {
        _fileStorage = fileStorage;
        _chunkingService = chunkingService;
        _aiGateway = aiGateway;
        _vectorStore = vectorStore;
        _textExtractor = textExtractor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DocumentIndexingResult> IndexDocumentAsync(
        DocumentIndexingRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            LogIndexingStarted(_logger, request.DocumentId, request.DocumentName);

            // Step 1: Validate content type
            if (!DocumentTextExtractor.IsSupported(request.ContentType))
            {
                var error = $"Unsupported content type for indexing: {request.ContentType}";
                LogUnsupportedContentType(_logger, request.DocumentName, request.ContentType);
                return DocumentIndexingResult.Failure(request.DocumentId, error);
            }

            // Step 2: Download document from MinIO
            LogDownloadingDocument(_logger, request.DocumentName, request.ObjectKey);
            var downloadResult = await _fileStorage.DownloadFileAsync(request.ObjectKey, cancellationToken: cancellationToken);
            if (downloadResult.IsFailure)
            {
                return DocumentIndexingResult.Failure(request.DocumentId,
                    $"Failed to download document: {downloadResult.Error}");
            }

            // Step 3: Extract text content
            LogExtractingText(_logger, request.DocumentName);
            var textContent = _textExtractor.ExtractText(
                downloadResult.Value!, request.ContentType, request.DocumentName);

            if (string.IsNullOrWhiteSpace(textContent))
            {
                return DocumentIndexingResult.Failure(request.DocumentId,
                    "Failed to extract text content from document.");
            }

            LogTextExtracted(_logger, request.DocumentName, textContent.Length);

            // Step 4: Chunk the document using sentence-aware strategy
            LogChunkingDocument(_logger, request.DocumentName);
            var chunks = _chunkingService.ChunkDocument(new DocumentChunkingRequest
            {
                Content = textContent,
                DocumentName = request.DocumentName,
                DocumentId = request.DocumentId,
                Category = request.Category
            });

            if (chunks.Count == 0)
            {
                return DocumentIndexingResult.Failure(request.DocumentId,
                    "Document chunking produced no chunks.");
            }

            LogChunksCreated(_logger, request.DocumentName, chunks.Count);

            // Step 5: Ensure Qdrant collection exists
            await _vectorStore.EnsureCollectionExistsAsync(
                request.CollectionName,
                1536, // Default for text-embedding-3-small; will be overridden by actual embedding size
                cancellationToken);

            // Step 6: Generate embeddings and store in batches
            var vectorPoints = new List<VectorPoint>();
            var embeddingModel = "unknown";
            var vectorDimensions = 0;

            var chunkBatches = chunks
                .Select((chunk, index) => new { chunk, index })
                .GroupBy(x => x.index / EmbeddingBatchSize)
                .Select(g => g.Select(x => x.chunk).ToList());

            foreach (var batch in chunkBatches)
            {
                foreach (var chunk in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Prepend contextual header to chunk text for embedding
                    // This improves retrieval accuracy as per RAG guidelines
                    var textToEmbed = $"{chunk.ContextualHeader}\n{chunk.Text}";

                    var embeddingResponse = await _aiGateway.GenerateEmbeddingAsync(
                        new AiEmbeddingRequest
                        {
                            TenantId = request.TenantId,
                            Text = textToEmbed
                        },
                        cancellationToken);

                    if (embeddingResponse.Embedding.Length == 0)
                    {
                        LogEmbeddingFailed(_logger, request.DocumentName, chunk.Index);
                        continue;
                    }

                    embeddingModel = embeddingResponse.Model ?? embeddingModel;
                    vectorDimensions = embeddingResponse.Embedding.Length;

                    vectorPoints.Add(new VectorPoint
                    {
                        Id = Guid.NewGuid(),
                        Vector = embeddingResponse.Embedding,
                        Payload = new VectorPointPayload
                        {
                            TenantId = request.TenantId,
                            DocumentId = request.DocumentId,
                            DocumentName = request.DocumentName,
                            ChunkText = chunk.Text,
                            ChunkIndex = chunk.Index,
                            ContextualHeader = chunk.ContextualHeader,
                            SectionName = chunk.SectionName,
                            PageNumbers = chunk.PageNumbers,
                            Category = request.Category,
                            IndexedAt = DateTime.UtcNow
                        }
                    });
                }
            }

            if (vectorPoints.Count == 0)
            {
                return DocumentIndexingResult.Failure(request.DocumentId,
                    "Failed to generate any embeddings for document chunks.");
            }

            // Ensure collection has correct vector size
            await _vectorStore.EnsureCollectionExistsAsync(
                request.CollectionName, vectorDimensions, cancellationToken);

            // Step 7: Upsert vectors into Qdrant
            LogStoringVectors(_logger, request.DocumentName, vectorPoints.Count);
            await _vectorStore.UpsertPointsAsync(request.CollectionName, vectorPoints, cancellationToken);

            stopwatch.Stop();

            var result = DocumentIndexingResult.Success(
                request.DocumentId,
                vectorPoints.Count,
                textContent.Length,
                embeddingModel,
                vectorDimensions,
                stopwatch.ElapsedMilliseconds);

            LogIndexingCompleted(_logger, request.DocumentId, request.DocumentName,
                vectorPoints.Count, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException)
        {
            LogIndexingCancelled(_logger, request.DocumentId, request.DocumentName);
            return DocumentIndexingResult.Failure(request.DocumentId, "Indexing was cancelled.");
        }
        catch (Exception ex)
        {
            LogIndexingFailed(_logger, ex, request.DocumentId, request.DocumentName);
            return DocumentIndexingResult.Failure(request.DocumentId,
                $"Indexing failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<DocumentIndexingResult> ReindexDocumentAsync(
        DocumentIndexingRequest request,
        CancellationToken cancellationToken = default)
    {
        LogReindexingStarted(_logger, request.DocumentId, request.DocumentName);

        // First, remove existing vectors for this document
        await RemoveDocumentAsync(request.CollectionName, request.DocumentId, cancellationToken);

        // Then, index the document fresh
        return await IndexDocumentAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveDocumentAsync(
        string collectionName,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        LogRemovingDocument(_logger, documentId, collectionName);
        await _vectorStore.DeleteByDocumentIdAsync(collectionName, documentId, cancellationToken);
        LogDocumentRemoved(_logger, documentId, collectionName);
    }

    // -------------------------------------------------------------------------
    // LoggerMessage delegates
    // -------------------------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting document indexing for {DocumentId} ({DocumentName})")]
    private static partial void LogIndexingStarted(ILogger logger, Guid documentId, string documentName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unsupported content type for document '{DocumentName}': {ContentType}")]
    private static partial void LogUnsupportedContentType(ILogger logger, string documentName, string contentType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Downloading document '{DocumentName}' from storage (key: {ObjectKey})")]
    private static partial void LogDownloadingDocument(ILogger logger, string documentName, string objectKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Extracting text from document '{DocumentName}'")]
    private static partial void LogExtractingText(ILogger logger, string documentName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Text extracted from '{DocumentName}': {TextLength} characters")]
    private static partial void LogTextExtracted(ILogger logger, string documentName, int textLength);

    [LoggerMessage(Level = LogLevel.Information, Message = "Chunking document '{DocumentName}'")]
    private static partial void LogChunkingDocument(ILogger logger, string documentName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created {ChunkCount} chunks for document '{DocumentName}'")]
    private static partial void LogChunksCreated(ILogger logger, string documentName, int chunkCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Embedding generation failed for chunk {ChunkIndex} of document '{DocumentName}'")]
    private static partial void LogEmbeddingFailed(ILogger logger, string documentName, int chunkIndex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Storing {VectorCount} vectors for document '{DocumentName}'")]
    private static partial void LogStoringVectors(ILogger logger, string documentName, int vectorCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Document indexing completed for {DocumentId} ({DocumentName}): {ChunkCount} chunks in {ElapsedMs}ms")]
    private static partial void LogIndexingCompleted(ILogger logger, Guid documentId, string documentName, int chunkCount, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Document indexing cancelled for {DocumentId} ({DocumentName})")]
    private static partial void LogIndexingCancelled(ILogger logger, Guid documentId, string documentName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Document indexing failed for {DocumentId} ({DocumentName})")]
    private static partial void LogIndexingFailed(ILogger logger, Exception ex, Guid documentId, string documentName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Re-indexing document {DocumentId} ({DocumentName})")]
    private static partial void LogReindexingStarted(ILogger logger, Guid documentId, string documentName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Removing document {DocumentId} from collection '{CollectionName}'")]
    private static partial void LogRemovingDocument(ILogger logger, Guid documentId, string collectionName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Document {DocumentId} removed from collection '{CollectionName}'")]
    private static partial void LogDocumentRemoved(ILogger logger, Guid documentId, string collectionName);
}
