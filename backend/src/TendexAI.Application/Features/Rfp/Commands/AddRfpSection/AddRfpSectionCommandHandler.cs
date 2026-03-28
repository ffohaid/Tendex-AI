using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.AddRfpSection;

/// <summary>
/// Handles adding a new section to a competition's RFP booklet.
/// </summary>
public sealed class AddRfpSectionCommandHandler
    : ICommandHandler<AddRfpSectionCommand, RfpSectionDto>
{
    private readonly ICompetitionRepository _repository;
    private readonly ILogger<AddRfpSectionCommandHandler> _logger;

    public AddRfpSectionCommandHandler(
        ICompetitionRepository repository,
        ILogger<AddRfpSectionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<RfpSectionDto>> Handle(
        AddRfpSectionCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _repository.GetByIdWithDetailsAsync(request.CompetitionId, cancellationToken);
        if (competition is null)
            return Result.Failure<RfpSectionDto>("Competition not found.");

        var section = RfpSection.Create(
            competitionId: request.CompetitionId,
            titleAr: request.TitleAr,
            titleEn: request.TitleEn,
            sectionType: request.SectionType,
            contentHtml: request.ContentHtml,
            isMandatory: request.IsMandatory,
            isFromTemplate: request.IsFromTemplate,
            defaultTextColor: request.DefaultTextColor,
            createdBy: request.CreatedByUserId,
            parentSectionId: request.ParentSectionId);

        var result = competition.AddSection(section);
        if (result.IsFailure)
            return Result.Failure<RfpSectionDto>(result.Error!);

        _repository.Update(competition);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogSectionAdded(section.Id, request.CompetitionId);

        return Result.Success(CompetitionMapper.ToSectionDto(section));
    }
}
