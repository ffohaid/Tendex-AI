using MediatR;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Features.Rfp.Queries.GetCompetitionTemplates;

public sealed record GetCompetitionTemplatesQuery(
    Guid TenantId,
    string? Category,
    string? SearchQuery,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<CompetitionTemplateListDto>>;

public sealed record CompetitionTemplateListDto(
    List<CompetitionTemplateDto> Items,
    int TotalCount);

public sealed record CompetitionTemplateDto(
    Guid Id,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string Category,
    int CompetitionType,
    bool IsOfficial,
    bool IsActive,
    int SectionCount,
    int BoqItemCount,
    int EvaluationCriteriaCount,
    int UsageCount,
    string? Tags,
    DateTime CreatedAt,
    string CreatedBy);
