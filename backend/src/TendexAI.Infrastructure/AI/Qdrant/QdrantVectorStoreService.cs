using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Infrastructure.AI.Qdrant;

/// <summary>
/// Qdrant implementation of <see cref="IVectorStoreService"/>.
/// Provides tenant-isolated vector storage and semantic search
/// for the RAG (Retrieval-Augmented Generation) engine.
/// Uses gRPC for high-performance communication with Qdrant.
/// </summary>
public sealed partial class QdrantVectorStoreService : IVectorStoreService
{
    private readonly QdrantClient _client;
    private readonly QdrantSettings _settings;
    private readonly ILogger<QdrantVectorStoreService> _logger;

    public QdrantVectorStoreService(
        QdrantClient client,
        IOptions<QdrantSettings> settings,
        ILogger<QdrantVectorStoreService> logger)
    {
        _client = client;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task EnsureCollectionExistsAsync(
        string collectionName,
        int vectorSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collections = await _client.ListCollectionsAsync(cancellationToken);
            if (collections.Any(c => c == collectionName))
            {
                LogCollectionAlreadyExists(_logger, collectionName);
                return;
            }

            LogCreatingCollection(_logger, collectionName, vectorSize);

            await _client.CreateCollectionAsync(
                collectionName,
                new VectorParams
                {
                    Size = (ulong)vectorSize,
                    Distance = Distance.Cosine,
                    OnDisk = true
                },
                cancellationToken: cancellationToken);

            // Create payload indexes for efficient filtering
            await _client.CreatePayloadIndexAsync(
                collectionName,
                "tenant_id",
                PayloadSchemaType.Keyword,
                cancellationToken: cancellationToken);

            await _client.CreatePayloadIndexAsync(
                collectionName,
                "document_id",
                PayloadSchemaType.Keyword,
                cancellationToken: cancellationToken);

            await _client.CreatePayloadIndexAsync(
                collectionName,
                "category",
                PayloadSchemaType.Keyword,
                cancellationToken: cancellationToken);

            LogCollectionCreated(_logger, collectionName);
        }
        catch (Exception ex)
        {
            LogCollectionCreationFailed(_logger, ex, collectionName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpsertPointsAsync(
        string collectionName,
        IReadOnlyList<VectorPoint> points,
        CancellationToken cancellationToken = default)
    {
        if (points.Count == 0) return;

        try
        {
            LogUpsertingPoints(_logger, points.Count, collectionName);

            // Process in batches to avoid exceeding gRPC message size limits
            var batches = points
                .Select((point, index) => new { point, index })
                .GroupBy(x => x.index / _settings.MaxBatchSize)
                .Select(g => g.Select(x => x.point).ToList());

            foreach (var batch in batches)
            {
                var qdrantPoints = batch.Select(MapToQdrantPoint).ToList();

                await _client.UpsertAsync(
                    collectionName,
                    qdrantPoints,
                    cancellationToken: cancellationToken);
            }

            LogPointsUpserted(_logger, points.Count, collectionName);
        }
        catch (Exception ex)
        {
            LogUpsertFailed(_logger, ex, points.Count, collectionName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            LogSearching(_logger, request.CollectionName, request.TopK);

            // Build filter conditions for tenant isolation
            var mustConditions = new List<Condition>
            {
                new()
                {
                    Field = new FieldCondition
                    {
                        Key = "tenant_id",
                        Match = new Match { Keyword = request.TenantId.ToString() }
                    }
                }
            };

            // Optional document ID filter
            if (request.DocumentIdFilter.HasValue)
            {
                mustConditions.Add(new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "document_id",
                        Match = new Match { Keyword = request.DocumentIdFilter.Value.ToString() }
                    }
                });
            }

            // Optional category filter
            if (!string.IsNullOrWhiteSpace(request.CategoryFilter))
            {
                mustConditions.Add(new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "category",
                        Match = new Match { Keyword = request.CategoryFilter }
                    }
                });
            }

            var filter = new Filter();
            filter.Must.AddRange(mustConditions);

            var searchResults = await _client.SearchAsync(
                request.CollectionName,
                request.QueryVector,
                filter: filter,
                limit: (ulong)request.TopK,
                scoreThreshold: request.ScoreThreshold,
                cancellationToken: cancellationToken);

            var results = searchResults
                .Select(MapToSearchResult)
                .Where(r => r is not null)
                .Cast<VectorSearchResult>()
                .ToList();

            LogSearchCompleted(_logger, results.Count, request.CollectionName);

            return results;
        }
        catch (Exception ex)
        {
            LogSearchFailed(_logger, ex, request.CollectionName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteByDocumentIdAsync(
        string collectionName,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            LogDeletingDocumentPoints(_logger, documentId, collectionName);

            var filter = new Filter();
            filter.Must.Add(new Condition
            {
                Field = new FieldCondition
                {
                    Key = "document_id",
                    Match = new Match { Keyword = documentId.ToString() }
                }
            });

            await _client.DeleteAsync(
                collectionName,
                filter,
                cancellationToken: cancellationToken);

            LogDocumentPointsDeleted(_logger, documentId, collectionName);
        }
        catch (Exception ex)
        {
            LogDeleteFailed(_logger, ex, documentId, collectionName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.ListCollectionsAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            LogHealthCheckFailed(_logger, ex);
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // Mapping helpers
    // -------------------------------------------------------------------------

    private static PointStruct MapToQdrantPoint(VectorPoint point)
    {
        var payload = new Dictionary<string, Value>
        {
            ["tenant_id"] = new() { StringValue = point.Payload.TenantId.ToString() },
            ["document_id"] = new() { StringValue = point.Payload.DocumentId.ToString() },
            ["document_name"] = new() { StringValue = point.Payload.DocumentName },
            ["chunk_text"] = new() { StringValue = point.Payload.ChunkText },
            ["chunk_index"] = new() { IntegerValue = point.Payload.ChunkIndex },
            ["contextual_header"] = new() { StringValue = point.Payload.ContextualHeader },
            ["indexed_at"] = new() { StringValue = point.Payload.IndexedAt.ToString("O") }
        };

        if (!string.IsNullOrWhiteSpace(point.Payload.SectionName))
            payload["section_name"] = new Value { StringValue = point.Payload.SectionName };

        if (!string.IsNullOrWhiteSpace(point.Payload.PageNumbers))
            payload["page_numbers"] = new Value { StringValue = point.Payload.PageNumbers };

        if (!string.IsNullOrWhiteSpace(point.Payload.Category))
            payload["category"] = new Value { StringValue = point.Payload.Category };

        var qdrantPoint = new PointStruct
        {
            Id = new PointId { Uuid = point.Id.ToString() },
            Vectors = point.Vector
        };

        foreach (var kvp in payload)
        {
            qdrantPoint.Payload.Add(kvp.Key, kvp.Value);
        }

        return qdrantPoint;
    }

    private static VectorSearchResult? MapToSearchResult(ScoredPoint scoredPoint)
    {
        var payload = scoredPoint.Payload;
        if (payload is null || !payload.ContainsKey("chunk_text"))
            return null;

        return new VectorSearchResult
        {
            Id = Guid.TryParse(scoredPoint.Id.Uuid, out var id) ? id : Guid.Empty,
            Score = scoredPoint.Score,
            Payload = new VectorPointPayload
            {
                TenantId = Guid.TryParse(GetPayloadString(payload, "tenant_id"), out var tenantId) ? tenantId : Guid.Empty,
                DocumentId = Guid.TryParse(GetPayloadString(payload, "document_id"), out var docId) ? docId : Guid.Empty,
                DocumentName = GetPayloadString(payload, "document_name") ?? string.Empty,
                ChunkText = GetPayloadString(payload, "chunk_text") ?? string.Empty,
                ChunkIndex = (int)(payload.TryGetValue("chunk_index", out var ci) ? ci.IntegerValue : 0),
                ContextualHeader = GetPayloadString(payload, "contextual_header") ?? string.Empty,
                SectionName = GetPayloadString(payload, "section_name"),
                PageNumbers = GetPayloadString(payload, "page_numbers"),
                Category = GetPayloadString(payload, "category"),
                IndexedAt = DateTime.TryParse(GetPayloadString(payload, "indexed_at"), out var indexedAt)
                    ? indexedAt
                    : DateTime.UtcNow
            }
        };
    }

    private static string? GetPayloadString(
        Google.Protobuf.Collections.MapField<string, Value> payload,
        string key)
    {
        return payload.TryGetValue(key, out var value) ? value.StringValue : null;
    }

    // -------------------------------------------------------------------------
    // High-performance LoggerMessage delegates (CA1848 compliant)
    // -------------------------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Debug, Message = "Collection '{CollectionName}' already exists in Qdrant")]
    private static partial void LogCollectionAlreadyExists(ILogger logger, string collectionName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating Qdrant collection '{CollectionName}' with vector size {VectorSize}")]
    private static partial void LogCreatingCollection(ILogger logger, string collectionName, int vectorSize);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully created Qdrant collection '{CollectionName}' with payload indexes")]
    private static partial void LogCollectionCreated(ILogger logger, string collectionName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to create Qdrant collection '{CollectionName}'")]
    private static partial void LogCollectionCreationFailed(ILogger logger, Exception ex, string collectionName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Upserting {PointCount} points into collection '{CollectionName}'")]
    private static partial void LogUpsertingPoints(ILogger logger, int pointCount, string collectionName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully upserted {PointCount} points into collection '{CollectionName}'")]
    private static partial void LogPointsUpserted(ILogger logger, int pointCount, string collectionName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to upsert {PointCount} points into collection '{CollectionName}'")]
    private static partial void LogUpsertFailed(ILogger logger, Exception ex, int pointCount, string collectionName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Searching collection '{CollectionName}' for top {TopK} results")]
    private static partial void LogSearching(ILogger logger, string collectionName, int topK);

    [LoggerMessage(Level = LogLevel.Information, Message = "Search completed: {ResultCount} results from collection '{CollectionName}'")]
    private static partial void LogSearchCompleted(ILogger logger, int resultCount, string collectionName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Search failed in collection '{CollectionName}'")]
    private static partial void LogSearchFailed(ILogger logger, Exception ex, string collectionName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleting points for document {DocumentId} from collection '{CollectionName}'")]
    private static partial void LogDeletingDocumentPoints(ILogger logger, Guid documentId, string collectionName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully deleted points for document {DocumentId} from collection '{CollectionName}'")]
    private static partial void LogDocumentPointsDeleted(ILogger logger, Guid documentId, string collectionName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete points for document {DocumentId} from collection '{CollectionName}'")]
    private static partial void LogDeleteFailed(ILogger logger, Exception ex, Guid documentId, string collectionName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Qdrant health check failed")]
    private static partial void LogHealthCheckFailed(ILogger logger, Exception ex);
}
