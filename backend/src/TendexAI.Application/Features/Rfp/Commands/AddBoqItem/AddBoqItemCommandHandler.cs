using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.AddBoqItem;

/// <summary>
/// Handles adding a new BOQ item to a competition.
/// </summary>
public sealed class AddBoqItemCommandHandler
    : ICommandHandler<AddBoqItemCommand, BoqItemDto>
{
    private readonly ICompetitionRepository _repository;
    private readonly ILogger<AddBoqItemCommandHandler> _logger;

    public AddBoqItemCommandHandler(
        ICompetitionRepository repository,
        ILogger<AddBoqItemCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<BoqItemDto>> Handle(
        AddBoqItemCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _repository.GetByIdWithDetailsForUpdateAsync(request.CompetitionId, cancellationToken);
        if (competition is null)
            return Result.Failure<BoqItemDto>("Competition not found.");

        var sortOrder = competition.BoqItems.Count + 1;

        var item = BoqItem.Create(
            competitionId: request.CompetitionId,
            itemNumber: request.ItemNumber,
            descriptionAr: request.DescriptionAr,
            descriptionEn: request.DescriptionEn,
            unit: request.Unit,
            quantity: request.Quantity,
            estimatedUnitPrice: request.EstimatedUnitPrice,
            category: request.Category,
            createdBy: request.CreatedByUserId,
            sortOrder: sortOrder);

        var result = competition.AddBoqItem(item);
        if (result.IsFailure)
            return Result.Failure<BoqItemDto>(result.Error!);

        // Entity is already tracked — no need to call Update()
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogBoqItemAdded(item.Id, request.CompetitionId);

        return Result.Success(CompetitionMapper.ToBoqItemDto(item));
    }
}
