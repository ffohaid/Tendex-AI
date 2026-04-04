using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Application.Features.Workflow.Services;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.ChangeCompetitionStatus;

/// <summary>
/// Handles status transitions for a competition.
/// Delegates to the appropriate domain method based on the target status.
/// Automatically initiates approval workflows when transitioning to PendingApproval.
/// </summary>
public sealed class ChangeCompetitionStatusCommandHandler
    : ICommandHandler<ChangeCompetitionStatusCommand, CompetitionDetailDto>
{
    private readonly ICompetitionRepository _repository;
    private readonly IApprovalWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ChangeCompetitionStatusCommandHandler> _logger;

    public ChangeCompetitionStatusCommandHandler(
        ICompetitionRepository repository,
        IApprovalWorkflowService workflowService,
        ICurrentUserService currentUser,
        ILogger<ChangeCompetitionStatusCommandHandler> logger)
    {
        _repository = repository;
        _workflowService = workflowService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<CompetitionDetailDto>> Handle(
        ChangeCompetitionStatusCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _repository.GetByIdWithDetailsForUpdateAsync(request.CompetitionId, cancellationToken);
        if (competition is null)
            return Result.Failure<CompetitionDetailDto>("Competition not found.");

        var previousStatus = competition.Status;

        var result = request.NewStatus switch
        {
            CompetitionStatus.UnderPreparation => competition.TransitionTo(CompetitionStatus.UnderPreparation, request.ChangedByUserId),
            CompetitionStatus.Draft => competition.TransitionTo(CompetitionStatus.Draft, request.ChangedByUserId, request.Reason),
            CompetitionStatus.PendingApproval => competition.SubmitForApproval(request.ChangedByUserId),
            CompetitionStatus.Approved => competition.Approve(request.ChangedByUserId),
            CompetitionStatus.Rejected => competition.Reject(request.ChangedByUserId, request.Reason ?? "No reason provided."),
            CompetitionStatus.Cancelled => competition.Cancel(request.ChangedByUserId, request.Reason ?? "No reason provided."),
            CompetitionStatus.Suspended => competition.Suspend(request.ChangedByUserId, request.Reason ?? "No reason provided."),
            _ => competition.TransitionTo(request.NewStatus, request.ChangedByUserId, request.Reason)
        };

        if (result.IsFailure)
            return Result.Failure<CompetitionDetailDto>(result.Error!);

        // Entity is already tracked — no need to call Update()
        await _repository.SaveChangesAsync(cancellationToken);

        // ── Auto-initiate approval workflow when transitioning to PendingApproval ──
        if (request.NewStatus == CompetitionStatus.PendingApproval && _currentUser.TenantId.HasValue)
        {
            try
            {
                var workflowResult = await _workflowService.InitiateWorkflowAsync(
                    competitionId: request.CompetitionId,
                    tenantId: _currentUser.TenantId.Value,
                    fromStatus: CompetitionStatus.PendingApproval,
                    toStatus: CompetitionStatus.Approved,
                    initiatedByUserId: request.ChangedByUserId,
                    cancellationToken: cancellationToken);

                if (workflowResult.IsSuccess)
                {
                    _logger.LogInformation(
                        "Auto-initiated approval workflow for competition {CompetitionId} with {StepCount} steps",
                        request.CompetitionId, workflowResult.Value!.TotalSteps);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to auto-initiate approval workflow for competition {CompetitionId}: {Error}",
                        request.CompetitionId, workflowResult.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error auto-initiating approval workflow for competition {CompetitionId}",
                    request.CompetitionId);
            }
        }

        var statusName = request.NewStatus.ToString();
        _logger.LogCompetitionStatusChanged(request.CompetitionId, statusName, request.ChangedByUserId);

        return Result.Success(CompetitionMapper.ToDetailDto(competition));
    }
}
