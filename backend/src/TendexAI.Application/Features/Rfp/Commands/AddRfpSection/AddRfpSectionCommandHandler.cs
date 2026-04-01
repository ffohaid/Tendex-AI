using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.AddRfpSection;

/// <summary>
/// Handles adding a new section to a competition's RFP booklet.
/// Includes retry logic with ClearChangeTracker for concurrency conflicts
/// caused by the Competition.Version concurrency token.
/// </summary>
public sealed class AddRfpSectionCommandHandler
    : ICommandHandler<AddRfpSectionCommand, RfpSectionDto>
{
    private const int MaxRetries = 5;
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
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                // Clear change tracker before each attempt to get fresh Version from DB
                _repository.ClearChangeTracker();

                var competition = await _repository.GetByIdWithDetailsForUpdateAsync(
                    request.CompetitionId, cancellationToken);

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

                await _repository.SaveChangesAsync(cancellationToken);

                _logger.LogSectionAdded(section.Id, request.CompetitionId);

                return Result.Success(CompetitionMapper.ToSectionDto(section));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(
                    "Concurrency conflict on attempt {Attempt}/{MaxRetries} adding section to competition {CompetitionId}: {Message}",
                    attempt, MaxRetries, request.CompetitionId, ex.Message);

                if (attempt == MaxRetries)
                {
                    _logger.LogError(ex,
                        "Failed to add section after {MaxRetries} retries for competition {CompetitionId}",
                        MaxRetries, request.CompetitionId);
                    return Result.Failure<RfpSectionDto>(
                        "فشل في حفظ القسم بسبب تعارض في البيانات. يرجى المحاولة مرة أخرى.");
                }

                // Exponential backoff with jitter
                var baseDelay = 100 * (int)Math.Pow(2, attempt - 1);
                var jitter = Random.Shared.Next(0, 50);
                await Task.Delay(baseDelay + jitter, cancellationToken);
            }
        }

        return Result.Failure<RfpSectionDto>("حدث خطأ غير متوقع.");
    }
}
