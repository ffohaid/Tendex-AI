using MediatR;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.ExtractBookletFromDocument;

/// <summary>
/// Command to extract structured booklet content from an uploaded document using AI.
/// Powers the "Upload &amp; Extract" (رفع واستخراج) creation method.
/// 
/// Flow:
/// 1. Receives the uploaded file bytes and metadata
/// 2. Extracts raw text from the document (PDF/Word)
/// 3. Sends extracted text to AI for structured parsing
/// 4. Returns structured booklet sections, BOQ items, and project metadata
/// </summary>
public sealed record ExtractBookletFromDocumentCommand : IRequest<ExtractBookletFromDocumentResult>
{
    /// <summary>The tenant making the request.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>The uploaded file stream.</summary>
    public required Stream FileStream { get; init; }

    /// <summary>The original file name.</summary>
    public required string FileName { get; init; }

    /// <summary>The MIME type of the file.</summary>
    public required string ContentType { get; init; }

    /// <summary>The file size in bytes.</summary>
    public required long FileSizeBytes { get; init; }

    /// <summary>The user who uploaded the file.</summary>
    public required string UploadedByUserId { get; init; }
}

/// <summary>
/// Result of the booklet extraction command.
/// </summary>
public sealed record ExtractBookletFromDocumentResult
{
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public BookletExtractionResult? Extraction { get; init; }

    /// <summary>The file ID stored in MinIO (for later reference).</summary>
    public Guid? UploadedFileId { get; init; }
}
