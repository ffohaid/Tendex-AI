using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.AddEvaluationCriterion;

/// <summary>
/// Handles adding a new evaluation criterion to a competition.
/// Uses direct DB insertion to bypass the Competition aggregate's concurrency token (Version).
/// This prevents DbUpdateConcurrencyException when step 2 saves multiple AI-suggested criteria sequentially.
/// </summary>
public sealed class AddEvaluationCriterionCommandHandler
    : ICommandHandler<AddEvaluationCriterionCommand, EvaluationCriterionDto>
{
    private readonly ICompetitionRepository _repository;
    private readonly ILogger<AddEvaluationCriterionCommandHandler> _logger;

    public AddEvaluationCriterionCommandHandler(
        ICompetitionRepository repository,
        ILogger<AddEvaluationCriterionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<EvaluationCriterionDto>> Handle(
        AddEvaluationCriterionCommand request,
        CancellationToken cancellationToken)
    {
        var isModifiable = await _repository.IsCompetitionModifiableAsync(
            request.CompetitionId,
            cancellationToken);

        if (!isModifiable)
        {
            var competition = await _repository.GetByIdAsync(request.CompetitionId, cancellationToken);
            if (competition is null)
                return Result.Failure<EvaluationCriterionDto>("Competition not found.");

            return Result.Failure<EvaluationCriterionDto>(
                "لا يمكن إضافة معايير التقييم: المنافسة ليست في حالة قابلة للتعديل.");
        }

        var currentCriteriaCount = await _repository.GetEvaluationCriteriaCountAsync(
            request.CompetitionId,
            cancellationToken);

        var criterion = EvaluationCriterion.Create(
            competitionId: request.CompetitionId,
            nameAr: request.NameAr,
            nameEn: request.NameEn,
            descriptionAr: request.DescriptionAr,
            descriptionEn: request.DescriptionEn,
            weightPercentage: request.WeightPercentage,
            minimumPassingScore: request.MinimumPassingScore,
            sortOrder: currentCriteriaCount + 1,
            createdBy: request.CreatedByUserId,
            parentCriterionId: request.ParentCriterionId);

        await _repository.AddEvaluationCriterionDirectAsync(criterion, cancellationToken);

        _logger.LogInformation(
            "Successfully added evaluation criterion {CriterionId} to competition {CompetitionId} via direct insertion (SortOrder={SortOrder})",
            criterion.Id,
            request.CompetitionId,
            criterion.SortOrder);

        _logger.LogEvaluationCriterionAdded(criterion.Id, request.CompetitionId);

        return Result.Success(CompetitionMapper.ToCriterionDto(criterion));
    }
}
