using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.CopyFromTemplate;

public sealed class CopyFromTemplateCommandHandler
    : IRequestHandler<CopyFromTemplateCommand, Result>
{
    private readonly ITenantDbContextFactory _dbFactory;
    private readonly ILogger<CopyFromTemplateCommandHandler> _logger;

    public CopyFromTemplateCommandHandler(
        ITenantDbContextFactory dbFactory,
        ILogger<CopyFromTemplateCommandHandler> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<Result> Handle(
        CopyFromTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var db = _dbFactory.CreateDbContext();

        var template = await db.GetDbSet<CompetitionTemplate>()
            .Include(t => t.Sections.OrderBy(s => s.SortOrder))
            .Include(t => t.BoqItems)
            .Include(t => t.EvaluationCriteria)
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.IsActive, cancellationToken);

        if (template is null)
            return Result.Failure("القالب غير موجود أو غير نشط.");

        // Create competition from template
        var competition = Competition.Create(
            request.TenantId,
            request.ProjectNameAr,
            request.ProjectNameEn,
            template.CompetitionType,
            RfpCreationMethod.FromTemplate,
            request.UserId,
            description: request.Description,
            sourceTemplateId: template.Id);

        // Copy sections
        foreach (var templateSection in template.Sections)
        {
            var section = RfpSection.Create(
                competition.Id,
                templateSection.TitleAr,
                templateSection.TitleEn,
                templateSection.SectionType,
                templateSection.ContentHtml,
                templateSection.IsMandatory,
                isFromTemplate: true,
                templateSection.DefaultTextColor,
                createdBy: request.UserId);

            competition.AddSection(section);
        }

        // Copy BOQ items
        foreach (var templateBoq in template.BoqItems)
        {
            var boqItem = BoqItem.Create(
                competition.Id,
                templateBoq.ItemNumber,
                templateBoq.DescriptionAr,
                templateBoq.DescriptionEn,
                templateBoq.Unit,
                templateBoq.Quantity,
                templateBoq.EstimatedUnitPrice,
                templateBoq.Category,
                createdBy: request.UserId);

            competition.AddBoqItem(boqItem);
        }

        // Copy evaluation criteria
        foreach (var templateCriterion in template.EvaluationCriteria)
        {
            var criterion = EvaluationCriterion.Create(
                competition.Id,
                templateCriterion.NameAr,
                templateCriterion.NameEn,
                descriptionAr: null,
                descriptionEn: null,
                weightPercentage: templateCriterion.Weight,
                minimumPassingScore: templateCriterion.MaxScore,
                sortOrder: 0,
                createdBy: request.UserId);

            competition.AddEvaluationCriterion(criterion);
        }

        // Increment template usage count
        template.IncrementUsageCount();

        db.GetDbSet<Competition>().Add(competition);
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Competition {CompetitionId} created from template {TemplateId} by {UserId}",
            competition.Id, template.Id, request.UserId);

        return Result.Success<Guid>(competition.Id);
    }
}
