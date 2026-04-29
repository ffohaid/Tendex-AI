using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.CreateCompetition;

/// <summary>
/// Handles the creation of a new competition in the tenant database.
/// </summary>
public sealed class CreateCompetitionCommandHandler
    : ICommandHandler<CreateCompetitionCommand, CompetitionDetailDto>
{
    private readonly ICompetitionRepository _repository;
    private readonly ILogger<CreateCompetitionCommandHandler> _logger;

    public CreateCompetitionCommandHandler(
        ICompetitionRepository repository,
        ILogger<CreateCompetitionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<CompetitionDetailDto>> Handle(
        CreateCompetitionCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.BookletNumber) &&
            await _repository.IsReferenceNumberInUseAsync(request.BookletNumber, cancellationToken: cancellationToken))
        {
            return Result.Failure<CompetitionDetailDto>("رقم الكراسة المدخل مستخدم مسبقاً.");
        }

        var competition = Competition.Create(
            tenantId: request.TenantId,
            projectNameAr: request.ProjectNameAr,
            projectNameEn: request.ProjectNameEn,
            competitionType: request.CompetitionType,
            creationMethod: request.CreationMethod,
            createdByUserId: request.CreatedByUserId,
            referenceNumber: request.BookletNumber,
            description: request.Description,
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
            sourceTemplateId: request.SourceTemplateId,
            sourceCompetitionId: request.SourceCompetitionId);

        await _repository.AddAsync(competition, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogCompetitionCreated(competition.Id, competition.ReferenceNumber ?? string.Empty, request.TenantId);

        // Re-fetch with details to return full DTO
        var created = await _repository.GetByIdWithDetailsAsync(competition.Id, cancellationToken);
        return Result.Success(CompetitionMapper.ToDetailDto(created!));
    }
}
