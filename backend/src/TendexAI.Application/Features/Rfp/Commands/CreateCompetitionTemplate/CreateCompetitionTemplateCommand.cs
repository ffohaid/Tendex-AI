using MediatR;
using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.CreateCompetitionTemplate;

/// <summary>
/// Command to create a new competition template, either from scratch or from an existing competition.
/// </summary>
public sealed record CreateCompetitionTemplateCommand(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string Category,
    CompetitionType CompetitionType,
    string? Tags,
    bool IsOfficial,
    Guid? SourceCompetitionId,
    string UserId,
    Guid TenantId
) : IRequest<Result>;
