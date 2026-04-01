using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.BatchAddRfpSections;

/// <summary>
/// Handles adding multiple RFP sections to a competition in a single transaction.
/// Uses "client wins" concurrency resolution to handle Version conflicts:
/// - Clears the change tracker before each retry
/// - Reloads the competition fresh from DB with current Version
/// - All sections are added in a single SaveChanges call
/// </summary>
public sealed class BatchAddRfpSectionsCommandHandler
    : ICommandHandler<BatchAddRfpSectionsCommand, IReadOnlyList<RfpSectionDto>>
{
    private const int MaxRetries = 5;
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
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                // CRITICAL: Clear change tracker before EVERY attempt (including first)
                // This ensures we always get a fresh entity with the current DB Version
                _repository.ClearChangeTracker();
                
                return await ExecuteAsync(request, cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(
                    "Concurrency conflict on attempt {Attempt}/{MaxRetries} for competition {CompetitionId}: {Message}",
                    attempt, MaxRetries, request.CompetitionId, ex.Message);

                if (attempt == MaxRetries)
                {
                    _logger.LogError(ex,
                        "Failed to add sections after {MaxRetries} retries for competition {CompetitionId}",
                        MaxRetries, request.CompetitionId);
                    return Result.Failure<IReadOnlyList<RfpSectionDto>>(
                        "فشل في حفظ الأقسام بسبب تعارض في البيانات. يرجى المحاولة مرة أخرى.");
                }

                // Exponential backoff delay before retry
                var delay = 100 * (int)Math.Pow(2, attempt - 1); // 100ms, 200ms, 400ms, 800ms, 1600ms
                await Task.Delay(delay, cancellationToken);
            }
        }

        return Result.Failure<IReadOnlyList<RfpSectionDto>>("حدث خطأ غير متوقع.");
    }

    private async Task<Result<IReadOnlyList<RfpSectionDto>>> ExecuteAsync(
        BatchAddRfpSectionsCommand request,
        CancellationToken cancellationToken)
    {
        // Load competition fresh from DB (change tracker was cleared before this call)
        var competition = await _repository.GetByIdWithDetailsForUpdateAsync(
            request.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<IReadOnlyList<RfpSectionDto>>("لم يتم العثور على المنافسة.");

        _logger.LogDebug(
            "Loaded competition {CompetitionId} with Version={Version}, Sections={SectionCount}",
            competition.Id, competition.Version, competition.Sections.Count);

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

        // Single SaveChanges call for all sections
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully added {Count} sections to competition {CompetitionId} (new Version={Version})",
            addedSections.Count, request.CompetitionId, competition.Version);

        var dtos = addedSections
            .Select(CompetitionMapper.ToSectionDto)
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<RfpSectionDto>>(dtos);
    }
}
