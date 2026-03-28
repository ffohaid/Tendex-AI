using FluentValidation;

namespace TendexAI.Application.Features.Rag.Commands.IndexDocument;

/// <summary>
/// Validates the <see cref="IndexDocumentCommand"/> before processing.
/// </summary>
public sealed class IndexDocumentCommandValidator : AbstractValidator<IndexDocumentCommand>
{
    public IndexDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId)
            .NotEmpty()
            .WithMessage("Document ID is required.");

        RuleFor(x => x.ObjectKey)
            .NotEmpty()
            .WithMessage("Object key is required.");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("Content type is required.");

        RuleFor(x => x.DocumentName)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Document name is required and must not exceed 500 characters.");

        RuleFor(x => x.CollectionName)
            .NotEmpty()
            .MaximumLength(200)
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("Collection name must contain only alphanumeric characters, hyphens, and underscores.");

        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required.");
    }
}
