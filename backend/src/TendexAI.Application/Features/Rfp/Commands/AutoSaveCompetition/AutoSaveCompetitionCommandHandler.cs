using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.AutoSaveCompetition;

/// <summary>
/// Handles auto-saving a competition draft.
/// Performs partial updates and records the auto-save timestamp.
/// Only works on competitions in Draft status.
/// </summary>
public sealed class AutoSaveCompetitionCommandHandler
    : ICommandHandler<AutoSaveCompetitionCommand, AutoSaveResultDto>
{
    private readonly ICompetitionRepository _repository;
    private readonly ILogger<AutoSaveCompetitionCommandHandler> _logger;

    public AutoSaveCompetitionCommandHandler(
        ICompetitionRepository repository,
        ILogger<AutoSaveCompetitionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<AutoSaveResultDto>> Handle(
        AutoSaveCompetitionCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _repository.GetByIdForUpdateAsync(request.CompetitionId, cancellationToken);
        if (competition is null)
            return Result.Failure<AutoSaveResultDto>("Competition not found.");

        if (competition.Status != CompetitionStatus.Draft && competition.Status != CompetitionStatus.UnderPreparation)
            return Result.Failure<AutoSaveResultDto>("Auto-save is only available for Draft or UnderPreparation competitions.");

        // Apply partial updates only for provided fields
        if (request.ProjectNameAr is not null || request.ProjectNameEn is not null)
        {
            var updateResult = competition.UpdateBasicInfo(
                projectNameAr: request.ProjectNameAr ?? competition.ProjectNameAr,
                projectNameEn: request.ProjectNameEn ?? competition.ProjectNameEn,
                description: request.Description ?? competition.Description,
                competitionType: request.CompetitionType ?? competition.CompetitionType,
                estimatedBudget: request.EstimatedBudget ?? competition.EstimatedBudget,
                submissionDeadline: request.SubmissionDeadline ?? competition.SubmissionDeadline,
                projectDurationDays: request.ProjectDurationDays ?? competition.ProjectDurationDays,
                modifiedBy: request.ModifiedByUserId);

            if (updateResult.IsFailure)
                return Result.Failure<AutoSaveResultDto>(updateResult.Error!);
        }

        // Record the auto-save with optional wizard step
        competition.RecordAutoSave(request.CurrentWizardStep);

        // Entity is already tracked by GetByIdForUpdateAsync — no need to call Update()
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogAutoSaveCompleted(competition.Id, competition.Version);

        return Result.Success(new AutoSaveResultDto(
            CompetitionId: competition.Id,
            Version: competition.Version,
            SavedAt: competition.LastAutoSavedAt!.Value));
    }
}
