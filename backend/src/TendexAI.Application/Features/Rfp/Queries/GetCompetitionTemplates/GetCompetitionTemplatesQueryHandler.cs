using MediatR;
using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Queries.GetCompetitionTemplates;

public sealed class GetCompetitionTemplatesQueryHandler
    : IRequestHandler<GetCompetitionTemplatesQuery, Result<CompetitionTemplateListDto>>
{
    private readonly ITenantDbContext _db;

    public GetCompetitionTemplatesQueryHandler(ITenantDbContext db)
    {
        _db = db;
    }

    public async Task<Result<CompetitionTemplateListDto>> Handle(
        GetCompetitionTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.GetDbSet<CompetitionTemplate>()
            .Where(t => t.TenantId == request.TenantId && t.IsActive);

        // Filter by category
        if (!string.IsNullOrWhiteSpace(request.Category) && request.Category != "all")
        {
            query = query.Where(t => t.Category == request.Category);
        }

        // Search by name or tags
        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            var search = $"%{request.SearchQuery}%";
            query = query.Where(t =>
                EF.Functions.Like(t.NameAr, search) ||
                EF.Functions.Like(t.NameEn, search) ||
                (t.Tags != null && EF.Functions.Like(t.Tags, search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.IsOfficial)
            .ThenByDescending(t => t.UsageCount)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new CompetitionTemplateDto(
                t.Id,
                t.NameAr,
                t.NameEn,
                t.DescriptionAr,
                t.DescriptionEn,
                t.Category,
                (int)t.CompetitionType,
                t.IsOfficial,
                t.IsActive,
                t.Sections.Count,
                t.BoqItems.Count,
                t.EvaluationCriteria.Count,
                t.UsageCount,
                t.Tags,
                t.CreatedAt,
                t.CreatedBy ?? ""))
            .ToListAsync(cancellationToken);

        return Result<CompetitionTemplateListDto>.Success(
            new CompetitionTemplateListDto(items, totalCount));
    }
}
