using MediatR;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Features.Rfp.Commands.CopyFromTemplate;

/// <summary>
/// Creates a new competition from an existing template.
/// Copies all sections, BOQ items, and evaluation criteria.
/// </summary>
public sealed record CopyFromTemplateCommand(
    Guid TemplateId,
    string ProjectNameAr,
    string ProjectNameEn,
    string? Description,
    string UserId,
    Guid TenantId
) : IRequest<Result>;
