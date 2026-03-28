using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Infrastructure.AI.Rag;

/// <summary>
/// Implements context retrieval for RAG using a multi-stage pipeline:
/// 1. Embed the user query using the AI Gateway
/// 2. Perform semantic search in Qdrant (retrieve initial candidates)
/// 3. Apply cross-encoder reranking using the AI Gateway for precision
/// 4. Format the top results into XML-tagged context for prompt injection
///
/// The formatted context follows the RAG guidelines structure:
/// &lt;documents&gt;
///   &lt;document index="1"&gt;
///     &lt;source&gt;Document Name - Section - Page&lt;/source&gt;
///     &lt;document_content&gt;Chunk text&lt;/document_content&gt;
///   &lt;/document&gt;
/// &lt;/documents&gt;
/// </summary>
public sealed partial class ContextRetrievalService : IContextRetrievalService
{
    private readonly IAiGateway _aiGateway;
    private readonly IVectorStoreService _vectorStore;
    private readonly ILogger<ContextRetrievalService> _logger;

    public ContextRetrievalService(
        IAiGateway aiGateway,
        IVectorStoreService vectorStore,
        ILogger<ContextRetrievalService> logger)
    {
        _aiGateway = aiGateway;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ContextRetrievalResult> RetrieveContextAsync(
        ContextRetrievalRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            LogRetrievalStarted(_logger, request.Query, request.CollectionName, request.TopK);

            // Step 1: Generate query embedding
            var embeddingResponse = await _aiGateway.GenerateEmbeddingAsync(
                new AiEmbeddingRequest
                {
                    TenantId = request.TenantId,
                    Text = request.Query
                },
                cancellationToken);

            if (embeddingResponse.Embedding.Length == 0)
            {
                return ContextRetrievalResult.Failure("Failed to generate query embedding.");
            }

            // Step 2: Perform semantic search in Qdrant
            var searchResults = await _vectorStore.SearchAsync(
                new VectorSearchRequest
                {
                    CollectionName = request.CollectionName,
                    QueryVector = embeddingResponse.Embedding,
                    TenantId = request.TenantId,
                    TopK = request.InitialCandidates,
                    ScoreThreshold = request.ScoreThreshold,
                    DocumentIdFilter = request.DocumentIdFilter,
                    CategoryFilter = request.CategoryFilter
                },
                cancellationToken);

            if (searchResults.Count == 0)
            {
                LogNoResultsFound(_logger, request.CollectionName);
                stopwatch.Stop();
                return ContextRetrievalResult.Success(
                    string.Empty, [], 0, stopwatch.ElapsedMilliseconds);
            }

            LogCandidatesRetrieved(_logger, searchResults.Count, request.CollectionName);

            // Step 3: Apply cross-encoder reranking using the AI Gateway
            var rerankedResults = await RerankResultsAsync(
                request.Query, searchResults, request.TopK, request.TenantId, cancellationToken);

            // Step 4: Format context for prompt injection
            var formattedContext = FormatContextForPrompt(rerankedResults);

            // Step 5: Map to RetrievedChunk DTOs
            var chunks = rerankedResults.Select(r => new RetrievedChunk
            {
                Text = r.Payload.ChunkText,
                Score = r.Score,
                DocumentName = r.Payload.DocumentName,
                DocumentId = r.Payload.DocumentId,
                SectionName = r.Payload.SectionName,
                PageNumbers = r.Payload.PageNumbers,
                ContextualHeader = r.Payload.ContextualHeader,
                ChunkIndex = r.Payload.ChunkIndex
            }).ToList();

            stopwatch.Stop();

            LogRetrievalCompleted(_logger, chunks.Count, searchResults.Count, stopwatch.ElapsedMilliseconds);

            return ContextRetrievalResult.Success(
                formattedContext, chunks, searchResults.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            LogRetrievalFailed(_logger, ex, request.CollectionName);
            return ContextRetrievalResult.Failure($"Context retrieval failed: {ex.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // Reranking
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reranks search results using the AI Gateway as a cross-encoder.
    /// Sends the query and each candidate chunk to the LLM for relevance scoring.
    /// Falls back to the original vector similarity scores if reranking fails.
    /// </summary>
    private async Task<IReadOnlyList<VectorSearchResult>> RerankResultsAsync(
        string query,
        IReadOnlyList<VectorSearchResult> candidates,
        int topK,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (candidates.Count <= topK)
            {
                // No need to rerank if we have fewer candidates than requested
                return candidates;
            }

            LogRerankingStarted(_logger, candidates.Count);

            // Build reranking prompt
            var rerankPrompt = BuildRerankPrompt(query, candidates);

            var completionResponse = await _aiGateway.GenerateCompletionAsync(
                new AiCompletionRequest
                {
                    TenantId = tenantId,
                    SystemPrompt = RerankSystemPrompt,
                    UserPrompt = rerankPrompt,
                    MaxTokensOverride = 500,
                    TemperatureOverride = 0.0
                },
                cancellationToken);

            if (string.IsNullOrWhiteSpace(completionResponse.Content))
            {
                LogRerankingFailed(_logger, "Empty response from AI Gateway");
                return candidates.Take(topK).ToList();
            }

            // Parse reranking response to get ordered indices
            var rankedIndices = ParseRerankResponse(completionResponse.Content, candidates.Count);

            if (rankedIndices.Count == 0)
            {
                LogRerankingFailed(_logger, "Failed to parse reranking response");
                return candidates.Take(topK).ToList();
            }

            var reranked = rankedIndices
                .Where(i => i >= 0 && i < candidates.Count)
                .Take(topK)
                .Select(i => candidates[i])
                .ToList();

            LogRerankingCompleted(_logger, reranked.Count);

            return reranked;
        }
        catch (Exception ex)
        {
            LogRerankingException(_logger, ex);
            // Fall back to original ordering
            return candidates.Take(topK).ToList();
        }
    }

    private static string BuildRerankPrompt(string query, IReadOnlyList<VectorSearchResult> candidates)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Query: {query}");
        sb.AppendLine();
        sb.AppendLine("Documents:");

        for (var i = 0; i < candidates.Count; i++)
        {
            sb.AppendLine($"[{i}] {candidates[i].Payload.ContextualHeader}");
            sb.AppendLine(candidates[i].Payload.ChunkText);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static List<int> ParseRerankResponse(string response, int maxIndex)
    {
        var indices = new List<int>();

        // Parse comma-separated or newline-separated indices
        var parts = response
            .Replace("[", "")
            .Replace("]", "")
            .Split([',', '\n', ' '], StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out var index) && index >= 0 && index < maxIndex)
            {
                if (!indices.Contains(index))
                    indices.Add(index);
            }
        }

        return indices;
    }

    private const string RerankSystemPrompt =
        """
        You are a document relevance ranker. Given a query and a list of document chunks,
        rank them by relevance to the query. Return ONLY the indices of the documents
        in order of relevance (most relevant first), separated by commas.
        Example output: 3, 1, 7, 0, 5
        Do not include any explanation, just the comma-separated indices.
        """;

    // -------------------------------------------------------------------------
    // Context formatting
    // -------------------------------------------------------------------------

    /// <summary>
    /// Formats retrieved chunks into XML-tagged context for prompt injection.
    /// Follows the RAG guidelines structure for optimal AI comprehension.
    /// </summary>
    private static string FormatContextForPrompt(IReadOnlyList<VectorSearchResult> results)
    {
        if (results.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("<documents>");

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            sb.AppendLine($"  <document index=\"{i + 1}\">");
            sb.AppendLine($"    <source>{result.Payload.ContextualHeader}</source>");
            sb.AppendLine($"    <document_content>{result.Payload.ChunkText}</document_content>");
            sb.AppendLine("  </document>");
        }

        sb.AppendLine("</documents>");

        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // LoggerMessage delegates
    // -------------------------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting context retrieval for query '{Query}' in collection '{CollectionName}' (top {TopK})")]
    private static partial void LogRetrievalStarted(ILogger logger, string query, string collectionName, int topK);

    [LoggerMessage(Level = LogLevel.Information, Message = "No results found in collection '{CollectionName}'")]
    private static partial void LogNoResultsFound(ILogger logger, string collectionName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {CandidateCount} candidates from collection '{CollectionName}'")]
    private static partial void LogCandidatesRetrieved(ILogger logger, int candidateCount, string collectionName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting reranking of {CandidateCount} candidates")]
    private static partial void LogRerankingStarted(ILogger logger, int candidateCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reranking failed: {Reason}")]
    private static partial void LogRerankingFailed(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reranking completed: {ResultCount} results selected")]
    private static partial void LogRerankingCompleted(ILogger logger, int resultCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reranking threw an exception, falling back to original ordering")]
    private static partial void LogRerankingException(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Context retrieval completed: {ResultCount} chunks from {TotalCandidates} candidates in {ElapsedMs}ms")]
    private static partial void LogRetrievalCompleted(ILogger logger, int resultCount, int totalCandidates, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Context retrieval failed in collection '{CollectionName}'")]
    private static partial void LogRetrievalFailed(ILogger logger, Exception ex, string collectionName);
}
