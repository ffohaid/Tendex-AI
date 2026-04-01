using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.AddRfpSection;

/// <summary>
/// Handles adding a new section to a competition's RFP booklet.
/// Uses direct DB insertion via DbContext.RfpSections to completely bypass
/// the Competition aggregate's concurrency token (Version).
/// This eliminates DbUpdateConcurrencyException when adding sections.
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
        // Validate competition exists and is modifiable without loading the full entity
        var isModifiable = await _repository.IsCompetitionModifiableAsync(
            request.CompetitionId, cancellationToken);

        if (!isModifiable)
        {
            var competition = await _repository.GetByIdAsync(request.CompetitionId, cancellationToken);
            if (competition is null)
                return Result.Failure<RfpSectionDto>("لم يتم العثور على المنافسة.");

            return Result.Failure<RfpSectionDto>(
                "لا يمكن إضافة أقسام: المنافسة ليست في حالة قابلة للتعديل.");
        }

        // Get current section count for sort order
        var currentSectionCount = await _repository.GetSectionCountAsync(
            request.CompetitionId, cancellationToken);

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

        section.SetSortOrder(currentSectionCount + 1);

        // Direct DB insertion - bypasses Competition aggregate and its concurrency token
        await _repository.AddSectionDirectAsync(section, cancellationToken);

        _logger.LogInformation(
            "Successfully added section {SectionId} to competition {CompetitionId} via direct insertion (SortOrder={SortOrder})",
            section.Id, request.CompetitionId, section.SortOrder);

        return Result.Success(CompetitionMapper.ToSectionDto(section));
    }
}
