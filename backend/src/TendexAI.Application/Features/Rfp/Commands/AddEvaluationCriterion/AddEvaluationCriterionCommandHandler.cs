using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.AddEvaluationCriterion;

/// <summary>
/// Handles adding a new evaluation criterion to a competition.
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
        var competition = await _repository.GetByIdWithDetailsAsync(request.CompetitionId, cancellationToken);
        if (competition is null)
            return Result.Failure<EvaluationCriterionDto>("Competition not found.");

        var sortOrder = competition.EvaluationCriteria.Count + 1;

        var criterion = EvaluationCriterion.Create(
            competitionId: request.CompetitionId,
            nameAr: request.NameAr,
            nameEn: request.NameEn,
            descriptionAr: request.DescriptionAr,
            descriptionEn: request.DescriptionEn,
            weightPercentage: request.WeightPercentage,
            minimumPassingScore: request.MinimumPassingScore,
            sortOrder: sortOrder,
            createdBy: request.CreatedByUserId,
            parentCriterionId: request.ParentCriterionId);

        var result = competition.AddEvaluationCriterion(criterion);
        if (result.IsFailure)
            return Result.Failure<EvaluationCriterionDto>(result.Error!);

        _repository.Update(competition);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogEvaluationCriterionAdded(criterion.Id, request.CompetitionId);

        return Result.Success(CompetitionMapper.ToCriterionDto(criterion));
    }
}
