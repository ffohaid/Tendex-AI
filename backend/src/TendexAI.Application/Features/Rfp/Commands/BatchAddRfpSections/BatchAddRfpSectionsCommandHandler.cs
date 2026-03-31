using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.BatchAddRfpSections;

/// <summary>
/// Handles adding multiple RFP sections to a competition in a single transaction.
/// This avoids the concurrency conflict (DbUpdateConcurrencyException) that occurs
/// when adding sections one-by-one, since the Competition.Version is an EF concurrency token.
/// </summary>
public sealed class BatchAddRfpSectionsCommandHandler
    : ICommandHandler<BatchAddRfpSectionsCommand, IReadOnlyList<RfpSectionDto>>
{
    private readonly ICompetitionRepository _repository;
    private readonly ILogger<BatchAddRfpSectionsCommandHandler> _logger;

    public BatchAddRfpSectionsCommandHandler(
        ICompetitionRepository repository,
        ILogger<BatchAddRfpSectionsCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<RfpSectionDto>>> Handle(
        BatchAddRfpSectionsCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _repository.GetByIdWithDetailsForUpdateAsync(
            request.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<IReadOnlyList<RfpSectionDto>>("لم يتم العثور على المنافسة.");

        // If ClearExisting is true, remove all existing sections first
        if (request.ClearExisting)
        {
            var existingIds = competition.Sections.Select(s => s.Id).ToList();
            foreach (var existingId in existingIds)
            {
                var removeResult = competition.RemoveSection(existingId);
                if (removeResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to remove existing section {SectionId}: {Error}",
                        existingId, removeResult.Error);
                }
            }
        }

        var addedSections = new List<RfpSection>();

        foreach (var input in request.Sections)
        {
            var section = RfpSection.Create(
                competitionId: request.CompetitionId,
                titleAr: input.TitleAr,
                titleEn: input.TitleEn,
                sectionType: input.SectionType,
                contentHtml: input.ContentHtml,
                isMandatory: input.IsMandatory,
                isFromTemplate: input.IsFromTemplate,
                defaultTextColor: input.DefaultTextColor,
                createdBy: request.CreatedByUserId,
                parentSectionId: input.ParentSectionId);

            var addResult = competition.AddSection(section);
            if (addResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to add section '{Title}': {Error}",
                    input.TitleAr, addResult.Error);
                return Result.Failure<IReadOnlyList<RfpSectionDto>>(addResult.Error!);
            }

            addedSections.Add(section);
        }

        // Single SaveChanges call for all sections — avoids concurrency conflicts
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully added {Count} sections to competition {CompetitionId}",
            addedSections.Count, request.CompetitionId);

        var dtos = addedSections
            .Select(CompetitionMapper.ToSectionDto)
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<RfpSectionDto>>(dtos);
    }
}
