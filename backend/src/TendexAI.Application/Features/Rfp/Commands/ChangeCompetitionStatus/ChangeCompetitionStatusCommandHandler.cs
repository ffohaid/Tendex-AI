using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.ChangeCompetitionStatus;

/// <summary>
/// Handles status transitions for a competition.
/// Delegates to the appropriate domain method based on the target status.
/// </summary>
public sealed class ChangeCompetitionStatusCommandHandler
    : ICommandHandler<ChangeCompetitionStatusCommand, CompetitionDetailDto>
{
    private readonly ICompetitionRepository _repository;
    private readonly ILogger<ChangeCompetitionStatusCommandHandler> _logger;

    public ChangeCompetitionStatusCommandHandler(
        ICompetitionRepository repository,
        ILogger<ChangeCompetitionStatusCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<CompetitionDetailDto>> Handle(
        ChangeCompetitionStatusCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _repository.GetByIdWithDetailsForUpdateAsync(request.CompetitionId, cancellationToken);
        if (competition is null)
            return Result.Failure<CompetitionDetailDto>("Competition not found.");

        var result = request.NewStatus switch
        {
            CompetitionStatus.PendingApproval => competition.SubmitForApproval(request.ChangedByUserId),
            CompetitionStatus.Approved => competition.Approve(request.ChangedByUserId),
            CompetitionStatus.Rejected => competition.Reject(request.ChangedByUserId, request.Reason ?? "No reason provided."),
            CompetitionStatus.Cancelled => competition.Cancel(request.ChangedByUserId, request.Reason ?? "No reason provided."),
            _ => Result.Failure($"Status transition to {request.NewStatus} is not supported via this command.")
        };

        if (result.IsFailure)
            return Result.Failure<CompetitionDetailDto>(result.Error!);

        // Entity is already tracked — no need to call Update()
        await _repository.SaveChangesAsync(cancellationToken);

        var statusName = request.NewStatus.ToString();
        _logger.LogCompetitionStatusChanged(request.CompetitionId, statusName, request.ChangedByUserId);

        return Result.Success(CompetitionMapper.ToDetailDto(competition));
    }
}
