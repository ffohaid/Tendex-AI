using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.UpdateCompetition;

/// <summary>
/// Handles updating the basic information of an existing competition.
/// </summary>
public sealed class UpdateCompetitionCommandHandler
    : ICommandHandler<UpdateCompetitionCommand, CompetitionDetailDto>
{
    private readonly ICompetitionRepository _repository;
    private readonly ILogger<UpdateCompetitionCommandHandler> _logger;

    public UpdateCompetitionCommandHandler(
        ICompetitionRepository repository,
        ILogger<UpdateCompetitionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<CompetitionDetailDto>> Handle(
        UpdateCompetitionCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _repository.GetByIdWithDetailsForUpdateAsync(request.CompetitionId, cancellationToken);
        if (competition is null)
            return Result.Failure<CompetitionDetailDto>("Competition not found.");

        if (!string.IsNullOrWhiteSpace(request.BookletNumber) &&
            await _repository.IsReferenceNumberInUseAsync(request.BookletNumber, request.CompetitionId, cancellationToken))
        {
            return Result.Failure<CompetitionDetailDto>("رقم الكراسة المدخل مستخدم مسبقاً.");
        }

        var result = competition.UpdateBasicInfo(
            projectNameAr: request.ProjectNameAr,
            projectNameEn: request.ProjectNameEn,
            description: request.Description,
            competitionType: request.CompetitionType,
            referenceNumber: request.BookletNumber,
            estimatedBudget: request.EstimatedBudget,
            bookletIssueDate: request.BookletIssueDate,
            inquiriesStartDate: request.InquiriesStartDate,
            inquiryPeriodDays: request.InquiryPeriodDays,
            offersStartDate: request.OffersStartDate,
            submissionDeadline: request.SubmissionDeadline,
            expectedAwardDate: request.ExpectedAwardDate,
            workStartDate: request.WorkStartDate,
            department: request.Department,
            fiscalYear: request.FiscalYear,
            modifiedBy: request.ModifiedByUserId);

        if (result.IsFailure)
            return Result.Failure<CompetitionDetailDto>(result.Error!);

        // Entity is already tracked — no need to call Update()
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogCompetitionUpdated(request.CompetitionId, request.ModifiedByUserId);

        return Result.Success(CompetitionMapper.ToDetailDto(competition));
    }
}
