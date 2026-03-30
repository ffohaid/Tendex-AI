using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.UpdateEvaluationSettings;

/// <summary>
/// Handles updating evaluation settings for a competition.
/// </summary>
public sealed class UpdateEvaluationSettingsCommandHandler
    : ICommandHandler<UpdateEvaluationSettingsCommand, CompetitionDetailDto>
{
    private readonly ICompetitionRepository _repository;
    private readonly ILogger<UpdateEvaluationSettingsCommandHandler> _logger;

    public UpdateEvaluationSettingsCommandHandler(
        ICompetitionRepository repository,
        ILogger<UpdateEvaluationSettingsCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<CompetitionDetailDto>> Handle(
        UpdateEvaluationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _repository.GetByIdWithDetailsForUpdateAsync(request.CompetitionId, cancellationToken);
        if (competition is null)
            return Result.Failure<CompetitionDetailDto>("Competition not found.");

        var result = competition.UpdateEvaluationSettings(
            technicalPassingScore: request.TechnicalPassingScore,
            technicalWeight: request.TechnicalWeight,
            financialWeight: request.FinancialWeight,
            modifiedBy: request.ModifiedByUserId);

        if (result.IsFailure)
            return Result.Failure<CompetitionDetailDto>(result.Error!);

        // Entity is already tracked — no need to call Update()
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogEvaluationSettingsUpdated(request.CompetitionId);

        return Result.Success(CompetitionMapper.ToDetailDto(competition));
    }
}
