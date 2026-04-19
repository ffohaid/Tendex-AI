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
        var competition = Competition.Create(
            tenantId: request.TenantId,
            projectNameAr: request.ProjectNameAr,
            projectNameEn: request.ProjectNameEn,
            competitionType: request.CompetitionType,
            creationMethod: request.CreationMethod,
            createdByUserId: request.CreatedByUserId,
            description: request.Description,
            sourceTemplateId: request.SourceTemplateId,
            sourceCompetitionId: request.SourceCompetitionId);

        // Set optional fields
        if (request.EstimatedBudget.HasValue || request.SubmissionDeadline.HasValue || request.ProjectDurationDays.HasValue
            || request.StartDate.HasValue || request.EndDate.HasValue || request.Department is not null || request.FiscalYear is not null)
        {
            competition.UpdateBasicInfo(
                projectNameAr: request.ProjectNameAr,
                projectNameEn: request.ProjectNameEn,
                description: request.Description,
                competitionType: request.CompetitionType,
                estimatedBudget: request.EstimatedBudget,
                submissionDeadline: request.SubmissionDeadline,
                projectDurationDays: request.ProjectDurationDays,
                startDate: request.StartDate,
                endDate: request.EndDate,
                department: request.Department,
                fiscalYear: request.FiscalYear,
                modifiedBy: request.CreatedByUserId);
        }

        await _repository.AddAsync(competition, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogCompetitionCreated(competition.Id, competition.ReferenceNumber, request.TenantId);

        // Re-fetch with details to return full DTO
        var created = await _repository.GetByIdWithDetailsAsync(competition.Id, cancellationToken);
        return Result.Success(CompetitionMapper.ToDetailDto(created!));
    }
}
