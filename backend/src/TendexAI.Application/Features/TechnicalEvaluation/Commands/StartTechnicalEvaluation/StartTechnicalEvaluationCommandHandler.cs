using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.StartTechnicalEvaluation;

public sealed class StartTechnicalEvaluationCommandHandler
    : ICommandHandler<StartTechnicalEvaluationCommand, TechnicalEvaluationDetailDto>
{
    private readonly ICompetitionRepository _competitionRepository;
    private readonly ITechnicalEvaluationRepository _evaluationRepository;
    private readonly ILogger<StartTechnicalEvaluationCommandHandler> _logger;

    public StartTechnicalEvaluationCommandHandler(
        ICompetitionRepository competitionRepository,
        ITechnicalEvaluationRepository evaluationRepository,
        ILogger<StartTechnicalEvaluationCommandHandler> logger)
    {
        _competitionRepository = competitionRepository;
        _evaluationRepository = evaluationRepository;
        _logger = logger;
    }

    public async Task<Result<TechnicalEvaluationDetailDto>> Handle(
        StartTechnicalEvaluationCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load competition with details
        var competition = await _competitionRepository.GetByIdWithDetailsAsync(
            request.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<TechnicalEvaluationDetailDto>("Competition not found.");

        // 2. Validate competition is in the correct phase
        if (competition.Status != CompetitionStatus.OffersClosed &&
            competition.Status != CompetitionStatus.TechnicalAnalysis)
            return Result.Failure<TechnicalEvaluationDetailDto>(
                "Competition must be in OffersClosed or TechnicalAnalysis status to start technical evaluation.");

        // 3. Check if evaluation already exists
        var existingEvaluation = await _evaluationRepository.ExistsForCompetitionAsync(
            request.CompetitionId, cancellationToken);

        if (existingEvaluation)
            return Result.Failure<TechnicalEvaluationDetailDto>(
                "A technical evaluation already exists for this competition.");

        // 4. Validate minimum passing score is set
        if (!competition.TechnicalPassingScore.HasValue)
            return Result.Failure<TechnicalEvaluationDetailDto>(
                "Technical passing score must be configured before starting evaluation.");

        // 5. Create technical evaluation
        var evaluation = Domain.Entities.Evaluation.TechnicalEvaluation.Create(
            competition.Id,
            competition.TenantId,
            request.CommitteeId,
            competition.TechnicalPassingScore.Value,
            request.StartedByUserId);

        var startResult = evaluation.Start(request.StartedByUserId);
        if (startResult.IsFailure)
            return Result.Failure<TechnicalEvaluationDetailDto>(startResult.Error!);

        // 6. Persist
        await _evaluationRepository.AddAsync(evaluation, cancellationToken);
        await _evaluationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Technical evaluation {EvaluationId} started for competition {CompetitionId} by {UserId}",
            evaluation.Id, competition.Id, request.StartedByUserId);

        // 7. Return DTO
        return Result.Success(MapToDto(evaluation));
    }

    private static TechnicalEvaluationDetailDto MapToDto(Domain.Entities.Evaluation.TechnicalEvaluation evaluation)
    {
        return new TechnicalEvaluationDetailDto(
            evaluation.Id,
            evaluation.CompetitionId,
            evaluation.CommitteeId,
            evaluation.Status,
            evaluation.MinimumPassingScore,
            evaluation.IsBlindEvaluationActive,
            evaluation.StartedAt,
            evaluation.CompletedAt,
            evaluation.ApprovedAt,
            evaluation.ApprovedBy,
            evaluation.RejectionReason,
            evaluation.CreatedAt);
    }
}
