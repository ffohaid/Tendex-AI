using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.BatchAddRfpSections;

/// <summary>
/// Handles adding multiple RFP sections to a competition in a single transaction.
/// Uses direct DB insertion via DbContext.RfpSections to completely bypass
/// the Competition aggregate's concurrency token (Version).
/// This eliminates DbUpdateConcurrencyException when adding sections.
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
        // Validate competition exists and is modifiable without loading the full entity
        var isModifiable = await _repository.IsCompetitionModifiableAsync(
            request.CompetitionId, cancellationToken);

        if (!isModifiable)
        {
            // Check if competition exists at all
            var competition = await _repository.GetByIdAsync(request.CompetitionId, cancellationToken);
            if (competition is null)
                return Result.Failure<IReadOnlyList<RfpSectionDto>>("لم يتم العثور على المنافسة.");

            return Result.Failure<IReadOnlyList<RfpSectionDto>>(
                "لا يمكن إضافة أقسام: المنافسة ليست في حالة قابلة للتعديل.");
        }

        // Get current section count for sort order calculation
        var currentSectionCount = await _repository.GetSectionCountAsync(
            request.CompetitionId, cancellationToken);

        _logger.LogInformation(
            "Adding {Count} sections to competition {CompetitionId} (current sections: {CurrentCount})",
            request.Sections.Count, request.CompetitionId, currentSectionCount);

        // Create all section entities with proper sort order
        var sections = new List<RfpSection>();
        var sortOrder = currentSectionCount;

        foreach (var input in request.Sections)
        {
            sortOrder++;
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

            section.SetSortOrder(sortOrder);
            sections.Add(section);
        }

        _logger.LogInformation(
            "Created {Count} RfpSection entities for competition {CompetitionId}. First section: Id={SectionId}, Title={Title}, CompetitionId={CompId}",
            sections.Count, request.CompetitionId,
            sections.FirstOrDefault()?.Id,
            sections.FirstOrDefault()?.TitleAr,
            sections.FirstOrDefault()?.CompetitionId);

        try
        {
            // Direct DB insertion - bypasses Competition aggregate and its concurrency token
            await _repository.AddSectionsDirectAsync(sections, cancellationToken);

            // Verify sections were actually saved
            var verifyCount = await _repository.GetSectionCountAsync(
                request.CompetitionId, cancellationToken);

            _logger.LogInformation(
                "Successfully added {Count} sections to competition {CompetitionId} via direct insertion. Verified section count: {VerifiedCount}",
                sections.Count, request.CompetitionId, verifyCount);

            if (verifyCount == 0)
            {
                _logger.LogError(
                    "CRITICAL: Sections were added but verification shows 0 sections for competition {CompetitionId}. This indicates a transaction/context issue.",
                    request.CompetitionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to add sections to competition {CompetitionId}: {ErrorMessage}",
                request.CompetitionId, ex.Message);
            return Result.Failure<IReadOnlyList<RfpSectionDto>>(
                $"فشل في حفظ الأقسام: {ex.Message}");
        }

        var dtos = sections
            .Select(CompetitionMapper.ToSectionDto)
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<RfpSectionDto>>(dtos);
    }
}
