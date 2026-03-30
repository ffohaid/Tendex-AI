using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.DeleteCompetition;

/// <summary>
/// Handles soft-deletion of a competition.
/// </summary>
public sealed class DeleteCompetitionCommandHandler
    : ICommandHandler<DeleteCompetitionCommand>
{
    private readonly ICompetitionRepository _repository;
    private readonly ILogger<DeleteCompetitionCommandHandler> _logger;

    public DeleteCompetitionCommandHandler(
        ICompetitionRepository repository,
        ILogger<DeleteCompetitionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteCompetitionCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _repository.GetByIdForUpdateAsync(request.CompetitionId, cancellationToken);
        if (competition is null)
            return Result.Failure("Competition not found.");

        var result = competition.SoftDelete(request.DeletedByUserId);
        if (result.IsFailure)
            return result;

        // Entity is already tracked by GetByIdForUpdateAsync
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogCompetitionDeleted(request.CompetitionId, request.DeletedByUserId);

        return Result.Success();
    }
}
