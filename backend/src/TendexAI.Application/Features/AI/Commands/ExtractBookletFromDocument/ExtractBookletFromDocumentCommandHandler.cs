using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.ExtractBookletFromDocument;

/// <summary>
/// Handles the ExtractBookletFromDocumentCommand.
/// Orchestrates: file upload → text extraction → AI parsing → structured result.
/// </summary>
public sealed class ExtractBookletFromDocumentCommandHandler
    : IRequestHandler<ExtractBookletFromDocumentCommand, ExtractBookletFromDocumentResult>
{
    private readonly IBookletExtractionService _extractionService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IDocumentTextExtractorService _textExtractor;
    private readonly ILogger<ExtractBookletFromDocumentCommandHandler> _logger;

    /// <summary>Maximum document text length to send to AI (approximately 200 pages).</summary>
    private const int MaxDocumentTextLength = 500_000;

    public ExtractBookletFromDocumentCommandHandler(
        IBookletExtractionService extractionService,
        IFileStorageService fileStorageService,
        IDocumentTextExtractorService textExtractor,
        ILogger<ExtractBookletFromDocumentCommandHandler> logger)
    {
        _extractionService = extractionService;
        _fileStorageService = fileStorageService;
        _textExtractor = textExtractor;
        _logger = logger;
    }

    public async Task<ExtractBookletFromDocumentResult> Handle(
        ExtractBookletFromDocumentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling ExtractBookletFromDocumentCommand: file '{FileName}' ({ContentType}, {Size} bytes) for tenant {TenantId}",
            request.FileName, request.ContentType, request.FileSizeBytes, request.TenantId);

        try
        {
            // 1. Validate file type
            if (!_textExtractor.IsSupported(request.ContentType))
            {
                return new ExtractBookletFromDocumentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "نوع الملف غير مدعوم. يرجى رفع ملف PDF أو Word فقط."
                };
            }

            // 2. Read file bytes
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await request.FileStream.CopyToAsync(ms, cancellationToken);
                fileBytes = ms.ToArray();
            }

            if (fileBytes.Length == 0)
            {
                return new ExtractBookletFromDocumentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "الملف المرفوع فارغ."
                };
            }

            // 3. Extract raw text from document
            var rawText = _textExtractor.ExtractText(fileBytes, request.ContentType, request.FileName);

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new ExtractBookletFromDocumentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "لم يتم استخراج أي نص من المستند. تأكد من أن الملف يحتوي على نص قابل للقراءة وليس صور فقط."
                };
            }

            _logger.LogInformation(
                "Extracted {TextLength} characters from document '{FileName}'",
                rawText.Length, request.FileName);

            // 4. Truncate if too long (to stay within AI token limits)
            if (rawText.Length > MaxDocumentTextLength)
            {
                _logger.LogWarning(
                    "Document text truncated from {Original} to {Max} characters",
                    rawText.Length, MaxDocumentTextLength);
                rawText = rawText[..MaxDocumentTextLength];
            }

            // 5. Count pages (approximate from page markers)
            var pageCount = rawText.Split("--- Page ", StringSplitOptions.RemoveEmptyEntries).Length;

            // 6. Upload file to MinIO for storage (non-blocking - don't fail if storage fails)
            string? uploadedObjectKey = null;
            try
            {
                var uploadRequest = new FileUploadRequest
                {
                    FileStream = new MemoryStream(fileBytes),
                    FileName = request.FileName,
                    ContentType = request.ContentType,
                    FileSize = request.FileSizeBytes,
                    TenantId = request.TenantId,
                    FolderPath = "rfp-uploads"
                };

                var uploadResult = await _fileStorageService.UploadFileAsync(uploadRequest, cancellationToken);

                if (uploadResult.IsSuccess)
                {
                    uploadedObjectKey = uploadResult.Value!.ObjectKey;
                    _logger.LogInformation("File uploaded to storage: {ObjectKey}", uploadedObjectKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upload file to storage, continuing with extraction");
            }

            // 7. Send to AI for structured extraction
            var extractionRequest = new BookletExtractionRequest
            {
                TenantId = request.TenantId,
                DocumentText = rawText,
                FileName = request.FileName,
                ContentType = request.ContentType,
                FileSizeBytes = request.FileSizeBytes,
                PageCount = pageCount > 0 ? pageCount : null
            };

            var extractionResult = await _extractionService.ExtractBookletAsync(
                extractionRequest, cancellationToken);

            if (extractionResult.IsFailure)
            {
                _logger.LogWarning(
                    "Booklet extraction failed for '{FileName}': {Error}",
                    request.FileName, extractionResult.Error);

                return new ExtractBookletFromDocumentResult
                {
                    IsSuccess = false,
                    ErrorMessage = extractionResult.Error
                };
            }

            _logger.LogInformation(
                "Booklet extraction completed for '{FileName}': {SectionCount} sections, " +
                "{BoqCount} BOQ items, confidence: {Confidence}%",
                request.FileName,
                extractionResult.Value!.Sections.Count,
                extractionResult.Value.BoqItems.Count,
                extractionResult.Value.ConfidenceScore);

            return new ExtractBookletFromDocumentResult
            {
                IsSuccess = true,
                Extraction = extractionResult.Value
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception during booklet extraction for '{FileName}'",
                request.FileName);

            return new ExtractBookletFromDocumentResult
            {
                IsSuccess = false,
                ErrorMessage = $"حدث خطأ غير متوقع أثناء استخراج المحتوى: {ex.Message}"
            };
        }
    }
}
