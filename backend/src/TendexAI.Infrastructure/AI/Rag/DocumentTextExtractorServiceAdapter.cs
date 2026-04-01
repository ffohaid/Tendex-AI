using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Infrastructure.AI.Rag;

/// <summary>
/// Adapter that wraps <see cref="DocumentTextExtractor"/> to implement
/// <see cref="IDocumentTextExtractorService"/> for Clean Architecture compliance.
/// </summary>
public sealed class DocumentTextExtractorServiceAdapter : IDocumentTextExtractorService
{
    private readonly DocumentTextExtractor _extractor;

    public DocumentTextExtractorServiceAdapter(ILogger<DocumentTextExtractor> logger)
    {
        _extractor = new DocumentTextExtractor(logger);
    }

    /// <inheritdoc />
    public string? ExtractText(byte[] fileBytes, string contentType, string fileName)
    {
        return _extractor.ExtractText(fileBytes, contentType, fileName);
    }

    /// <inheritdoc />
    public bool IsSupported(string contentType)
    {
        return DocumentTextExtractor.IsSupported(contentType);
    }
}
