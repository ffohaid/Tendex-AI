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
        var isModifiable = await _repository.IsCompetitionModifiableAsync(
            request.CompetitionId,
            cancellationToken);

        if (!isModifiable)
        {
            var competition = await _repository.GetByIdAsync(request.CompetitionId, cancellationToken);
            if (competition is null)
                return Result.Failure<BoqItemDto>("Competition not found.");

            return Result.Failure<BoqItemDto>(
                "لا يمكن إضافة بند جدول كميات: المنافسة ليست في حالة قابلة للتعديل.");
        }

        var currentBoqCount = await _repository.GetBoqItemCountAsync(
            request.CompetitionId,
            cancellationToken);

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
            sortOrder: currentBoqCount + 1);

        await _repository.AddBoqItemDirectAsync(item, cancellationToken);

        _logger.LogInformation(
            "Successfully added BOQ item {BoqItemId} to competition {CompetitionId} via direct insertion (SortOrder={SortOrder})",
            item.Id,
            request.CompetitionId,
            item.SortOrder);

        _logger.LogBoqItemAdded(item.Id, request.CompetitionId);

        return Result.Success(CompetitionMapper.ToBoqItemDto(item));
    }
}
