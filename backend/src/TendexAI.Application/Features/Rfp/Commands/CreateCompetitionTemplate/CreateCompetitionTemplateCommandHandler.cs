using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.CreateCompetitionTemplate;

public sealed class CreateCompetitionTemplateCommandHandler
    : IRequestHandler<CreateCompetitionTemplateCommand, Result>
{
    private readonly ITenantDbContextFactory _dbFactory;
    private readonly ILogger<CreateCompetitionTemplateCommandHandler> _logger;

    public CreateCompetitionTemplateCommandHandler(
        ITenantDbContextFactory dbFactory,
        ILogger<CreateCompetitionTemplateCommandHandler> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<Result> Handle(
        CreateCompetitionTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var db = _dbFactory.CreateDbContext();

        CompetitionTemplate template;

        if (request.SourceCompetitionId.HasValue)
        {
            // Create from existing competition
            var competition = await db.GetDbSet<Competition>()
                .Include(c => c.Sections)
                .Include(c => c.BoqItems)
                .Include(c => c.EvaluationCriteria)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.SourceCompetitionId.Value, cancellationToken);

            if (competition is null)
                return Result.Failure("المنافسة المصدر غير موجودة.");

            template = CompetitionTemplate.CreateFromCompetition(
                request.TenantId,
                competition,
                request.NameAr,
                request.NameEn,
                request.DescriptionAr,
                request.DescriptionEn,
                request.Category,
                request.UserId);
        }
        else
        {
            // Create from scratch
            template = CompetitionTemplate.Create(
                request.TenantId,
                request.NameAr,
                request.NameEn,
                request.DescriptionAr,
                request.DescriptionEn,
                request.Category,
                request.CompetitionType,
                request.UserId,
                request.IsOfficial);
        }

        db.GetDbSet<CompetitionTemplate>().Add(template);
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Competition template {TemplateId} created by {UserId} for tenant {TenantId}",
            template.Id, request.UserId, request.TenantId);

        return Result.Success<Guid>(template.Id);
    }
}
