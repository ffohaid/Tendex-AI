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
/// Works on competitions in Draft, UnderPreparation, or Rejected status.
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

        var editableStatuses = new[]
        {
            CompetitionStatus.Draft,
            CompetitionStatus.UnderPreparation,
            CompetitionStatus.Rejected
        };
        if (!editableStatuses.Contains(competition.Status))
            return Result.Failure<AutoSaveResultDto>(
                $"Auto-save is only available for Draft, UnderPreparation, or Rejected competitions. Current status: {competition.Status}.");

        if (!string.IsNullOrWhiteSpace(request.BookletNumber) &&
            await _repository.IsReferenceNumberInUseAsync(request.BookletNumber, request.CompetitionId, cancellationToken))
        {
            return Result.Failure<AutoSaveResultDto>("رقم الكراسة المدخل مستخدم مسبقاً.");
        }

        // Apply partial updates only for provided fields
        if (request.ProjectNameAr is not null
            || request.ProjectNameEn is not null
            || request.Description is not null
            || request.CompetitionType.HasValue
            || request.BookletNumber is not null
            || request.EstimatedBudget.HasValue
            || request.BookletIssueDate.HasValue
            || request.InquiriesStartDate.HasValue
            || request.InquiryPeriodDays.HasValue
            || request.OffersStartDate.HasValue
            || request.SubmissionDeadline.HasValue
            || request.ExpectedAwardDate.HasValue
            || request.WorkStartDate.HasValue
            || request.Department is not null
            || request.FiscalYear is not null)
        {
            var updateResult = competition.UpdateBasicInfo(
                projectNameAr: request.ProjectNameAr ?? competition.ProjectNameAr,
                projectNameEn: request.ProjectNameEn ?? competition.ProjectNameEn,
                description: request.Description ?? competition.Description,
                competitionType: request.CompetitionType ?? competition.CompetitionType,
                referenceNumber: request.BookletNumber ?? competition.ReferenceNumber,
                estimatedBudget: request.EstimatedBudget ?? competition.EstimatedBudget,
                bookletIssueDate: request.BookletIssueDate ?? competition.StartDate,
                inquiriesStartDate: request.InquiriesStartDate ?? competition.InquiriesStartDate,
                inquiryPeriodDays: request.InquiryPeriodDays ?? competition.InquiryPeriodDays,
                offersStartDate: request.OffersStartDate ?? competition.OffersStartDate,
                submissionDeadline: request.SubmissionDeadline ?? competition.SubmissionDeadline,
                expectedAwardDate: request.ExpectedAwardDate ?? competition.ExpectedAwardDate,
                workStartDate: request.WorkStartDate ?? competition.WorkStartDate,
                department: request.Department ?? competition.Department,
                fiscalYear: request.FiscalYear ?? competition.FiscalYear,
                modifiedBy: request.ModifiedByUserId);

            if (updateResult.IsFailure)
                return Result.Failure<AutoSaveResultDto>(updateResult.Error!);
        }

        if (request.RequiredAttachmentTypes is not null)
        {
            var attachmentTypesResult = competition.UpdateRequiredAttachmentTypes(
                request.RequiredAttachmentTypes,
                request.ModifiedByUserId);

            if (attachmentTypesResult.IsFailure)
                return Result.Failure<AutoSaveResultDto>(attachmentTypesResult.Error!);
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
