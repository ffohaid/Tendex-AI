using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Domain.StateMachine;

namespace TendexAI.Application.Features.Rfp.Commands.TransitionCompetition;

/// <summary>
/// Handles competition status transitions using the state machine.
/// Validates state machine rules, prerequisites, and user permissions
/// before executing the transition.
/// </summary>
public sealed class TransitionCompetitionCommandHandler
    : ICommandHandler<TransitionCompetitionCommand, CompetitionTransitionResultDto>
{
    private readonly ICompetitionRepository _competitionRepository;
    private readonly ICompetitionPermissionService _permissionService;
    private readonly IPhasePrerequisiteContextFactory _prerequisiteContextFactory;
    private readonly CompetitionTransitionService _transitionService;
    private readonly ILogger<TransitionCompetitionCommandHandler> _logger;

    public TransitionCompetitionCommandHandler(
        ICompetitionRepository competitionRepository,
        ICompetitionPermissionService permissionService,
        IPhasePrerequisiteContextFactory prerequisiteContextFactory,
        CompetitionTransitionService transitionService,
        ILogger<TransitionCompetitionCommandHandler> logger)
    {
        _competitionRepository = competitionRepository;
        _permissionService = permissionService;
        _prerequisiteContextFactory = prerequisiteContextFactory;
        _transitionService = transitionService;
        _logger = logger;
    }

    public async Task<Result<CompetitionTransitionResultDto>> Handle(
        TransitionCompetitionCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load competition
        var competition = await _competitionRepository.GetByIdWithDetailsForUpdateAsync(
            request.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<CompetitionTransitionResultDto>("Competition not found.");

        var previousStatus = competition.Status;
        var previousPhase = competition.CurrentPhase;

        // 2. Validate user permissions
        var permissionResult = await _permissionService.ValidateTransitionPermissionAsync(
            request.CompetitionId,
            competition.Status,
            request.TargetStatus,
            request.ChangedByUserId,
            cancellationToken);

        if (permissionResult.IsFailure)
            return Result.Failure<CompetitionTransitionResultDto>(permissionResult.Error!);

        // 3. Build prerequisite context and validate
        var context = await _prerequisiteContextFactory.CreateAsync(
            competition, cancellationToken);

        var transitionResult = CompetitionTransitionService.ValidateTransition(
            competition.Status, request.TargetStatus, context);

        if (transitionResult.IsFailure)
            return Result.Failure<CompetitionTransitionResultDto>(transitionResult.Error!);

        // 4. Execute the transition on the aggregate
        var domainResult = competition.TransitionTo(
            request.TargetStatus, request.ChangedByUserId, request.Reason);

        if (domainResult.IsFailure)
            return Result.Failure<CompetitionTransitionResultDto>(domainResult.Error!);

        // 5. Create transition history record
        var history = PhaseTransitionHistory.Create(
            competition.Id,
            competition.TenantId,
            previousStatus,
            request.TargetStatus,
            previousPhase,
            competition.CurrentPhase,
            request.ChangedByUserId,
            request.Reason);

        // 6. Persist changes
        // Entity is already tracked — no need to call Update()
        await _competitionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Competition {CompetitionId} transitioned from {PreviousStatus} to {NewStatus} by {UserId}",
            competition.Id, previousStatus, request.TargetStatus, request.ChangedByUserId);

        // 7. Build response
        var allowedNext = CompetitionStateMachine.GetAllowedTransitions(competition.Status)
            .Select(s => new AllowedTransitionDto(
                s,
                s.ToString(),
                CompetitionStateMachine.GetPhase(s),
                CompetitionStateMachine.GetPhaseNameAr(CompetitionStateMachine.GetPhase(s)),
                CompetitionStateMachine.GetPhaseNameEn(CompetitionStateMachine.GetPhase(s))))
            .ToList();

        return Result.Success(new CompetitionTransitionResultDto(
            competition.Id,
            previousStatus,
            competition.Status,
            previousPhase,
            competition.CurrentPhase,
            CompetitionStateMachine.GetPhaseNameAr(competition.CurrentPhase),
            CompetitionStateMachine.GetPhaseNameEn(competition.CurrentPhase),
            DateTime.UtcNow,
            allowedNext));
    }
}

/// <summary>
/// Factory for creating prerequisite evaluation contexts.
/// Implemented in the infrastructure layer to access real data.
/// </summary>
public interface IPhasePrerequisiteContextFactory
{
    Task<IPhasePrerequisiteContext> CreateAsync(
        Competition competition,
        CancellationToken cancellationToken = default);
}
