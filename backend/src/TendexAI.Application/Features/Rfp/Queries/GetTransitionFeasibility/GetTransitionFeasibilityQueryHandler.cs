using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Commands.TransitionCompetition;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.StateMachine;

namespace TendexAI.Application.Features.Rfp.Queries.GetTransitionFeasibility;

/// <summary>
/// Handles the transition feasibility query.
/// Returns a detailed report of whether a transition is possible
/// and which prerequisites are satisfied or not.
/// </summary>
public sealed class GetTransitionFeasibilityQueryHandler
    : IQueryHandler<GetTransitionFeasibilityQuery, TransitionFeasibilityDto>
{
    private readonly ICompetitionRepository _competitionRepository;
    private readonly IPhasePrerequisiteContextFactory _prerequisiteContextFactory;
    private readonly CompetitionTransitionService _transitionService;

    public GetTransitionFeasibilityQueryHandler(
        ICompetitionRepository competitionRepository,
        IPhasePrerequisiteContextFactory prerequisiteContextFactory,
        CompetitionTransitionService transitionService)
    {
        _competitionRepository = competitionRepository;
        _prerequisiteContextFactory = prerequisiteContextFactory;
        _transitionService = transitionService;
    }

    public async Task<Result<TransitionFeasibilityDto>> Handle(
        GetTransitionFeasibilityQuery request,
        CancellationToken cancellationToken)
    {
        var competition = await _competitionRepository.GetByIdWithDetailsAsync(
            request.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<TransitionFeasibilityDto>("Competition not found.");

        var context = await _prerequisiteContextFactory.CreateAsync(competition, cancellationToken);

        var details = CompetitionTransitionService.GetTransitionDetails(
            competition.Status, request.TargetStatus, context);

        var dto = new TransitionFeasibilityDto(
            details.IsAllowed,
            details.IsStateMachineValid,
            details.ArePrerequisitesMet,
            details.CurrentStatus,
            details.TargetStatus,
            details.CurrentPhase,
            details.TargetPhase,
            details.Prerequisites.Select(p => new PrerequisiteCheckDto(
                p.Code, p.DescriptionAr, p.DescriptionEn, p.IsSatisfied)).ToList(),
            details.AllowedTransitions.Select(t => new AllowedTransitionDto(
                t.Status, t.Status.ToString(), t.Phase, t.PhaseNameAr, t.PhaseNameEn)).ToList());

        return Result.Success(dto);
    }
}
