using MediatR;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Application.Features.Rag.Commands.IndexDocument;
using TendexAI.Application.Features.Rag.Commands.RemoveDocument;
using TendexAI.Application.Features.Rag.Queries.GetVectorStoreStatus;
using TendexAI.Application.Features.Rag.Queries.RetrieveContext;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.AI;

/// <summary>
/// Minimal API endpoints for the RAG (Retrieval-Augmented Generation) engine.
/// Provides document indexing, context retrieval, and vector store management.
/// </summary>
public static class RagEndpoints
{
    public static void MapRagEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/rag")
            .WithTags("RAG Engine")
            .RequireAuthorization();

        // POST /api/v1/rag/index — Index a document into the vector database
        group.MapPost("/index", IndexDocumentHandler)
            .WithName("IndexDocument")
            .WithSummary("Index a document into the vector database for RAG retrieval")
            .WithDescription(
                "Triggers the full indexing pipeline: download from MinIO → extract text → " +
                "chunk with sentence-aware strategy → generate embeddings → store in Qdrant. " +
                "If the document was previously indexed, existing vectors are replaced (idempotent).")
            .Produces<DocumentIndexingResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PermissionPolicies.KnowledgeBaseManage);

        // POST /api/v1/rag/query — Retrieve relevant context for a query
        group.MapPost("/query", RetrieveContextHandler)
            .WithName("RetrieveContext")
            .WithSummary("Retrieve relevant document context for a RAG query")
            .WithDescription(
                "Performs semantic search in the vector database, applies cross-encoder reranking, " +
                "and returns formatted context ready for AI prompt injection. " +
                "Results are filtered by tenant for multi-tenant isolation.")
            .Produces<ContextRetrievalResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // DELETE /api/v1/rag/documents/{documentId} — Remove a document from the index
        group.MapDelete("/documents/{documentId:guid}", RemoveDocumentHandler)
            .WithName("RemoveDocumentFromIndex")
            .WithSummary("Remove all indexed vectors for a specific document")
            .WithDescription(
                "Deletes all vector points associated with the specified document " +
                "from the Qdrant collection. Used when a document is updated or removed.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // GET /api/v1/rag/health — Check vector store health
        group.MapGet("/health", GetVectorStoreStatusHandler)
            .WithName("GetVectorStoreStatus")
            .WithSummary("Check the health status of the vector store (Qdrant)")
            .Produces<VectorStoreStatusDto>(StatusCodes.Status200OK)
            .AllowAnonymous();
    }

    // -------------------------------------------------------------------------
    // Handler methods
    // -------------------------------------------------------------------------

    private static async Task<IResult> IndexDocumentHandler(
        IndexDocumentRequest request,
        IMediator mediator)
    {
        var command = new IndexDocumentCommand
        {
            DocumentId = request.DocumentId,
            ObjectKey = request.ObjectKey,
            ContentType = request.ContentType,
            DocumentName = request.DocumentName,
            CollectionName = request.CollectionName,
            TenantId = request.TenantId,
            Category = request.Category
        };

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                detail: result.ErrorMessage,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Document indexing failed");
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> RetrieveContextHandler(
        RetrieveContextRequest request,
        IMediator mediator)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.Problem(
                detail: "Query text is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request");
        }

        var query = new RetrieveContextQuery
        {
            Query = request.Query,
            TenantId = request.TenantId,
            CollectionName = request.CollectionName,
            TopK = request.TopK ?? 5,
            ScoreThreshold = request.ScoreThreshold ?? 0.5f,
            DocumentIdFilter = request.DocumentIdFilter,
            CategoryFilter = request.CategoryFilter
        };

        var result = await mediator.Send(query);

        if (!result.IsSuccess)
        {
            return Results.Problem(
                detail: result.ErrorMessage,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Context retrieval failed");
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> RemoveDocumentHandler(
        Guid documentId,
        string collectionName,
        IMediator mediator)
    {
        var command = new RemoveDocumentCommand
        {
            CollectionName = collectionName,
            DocumentId = documentId
        };

        var success = await mediator.Send(command);

        if (!success)
        {
            return Results.Problem(
                detail: "Failed to remove document from index.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Document removal failed");
        }

        return Results.Ok(new { DocumentId = documentId, Removed = true });
    }

    private static async Task<IResult> GetVectorStoreStatusHandler(IMediator mediator)
    {
        var status = await mediator.Send(new GetVectorStoreStatusQuery());
        return Results.Ok(status);
    }
}

// -------------------------------------------------------------------------
// Request DTOs
// -------------------------------------------------------------------------

/// <summary>
/// Request body for document indexing.
/// </summary>
public sealed record IndexDocumentRequest
{
    /// <summary>The unique identifier of the document to index.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>The MinIO object key for the document file.</summary>
    public required string ObjectKey { get; init; }

    /// <summary>The MIME type of the document.</summary>
    public required string ContentType { get; init; }

    /// <summary>The display name of the document.</summary>
    public required string DocumentName { get; init; }

    /// <summary>The Qdrant collection name for storage.</summary>
    public required string CollectionName { get; init; }

    /// <summary>The tenant that owns this document.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Optional document category.</summary>
    public string? Category { get; init; }
}

/// <summary>
/// Request body for context retrieval.
/// </summary>
public sealed record RetrieveContextRequest
{
    /// <summary>The user's query or question.</summary>
    public required string Query { get; init; }

    /// <summary>The tenant identifier.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>The Qdrant collection to search.</summary>
    public required string CollectionName { get; init; }

    /// <summary>Maximum number of context chunks to return.</summary>
    public int? TopK { get; init; }

    /// <summary>Minimum similarity score threshold.</summary>
    public float? ScoreThreshold { get; init; }

    /// <summary>Optional: filter by specific document ID.</summary>
    public Guid? DocumentIdFilter { get; init; }

    /// <summary>Optional: filter by document category.</summary>
    public string? CategoryFilter { get; init; }
}
