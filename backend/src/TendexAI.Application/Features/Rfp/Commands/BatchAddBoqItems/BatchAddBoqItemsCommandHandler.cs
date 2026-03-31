using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.BatchAddBoqItems;

/// <summary>
/// Handles adding multiple BOQ items to a competition in a single transaction.
/// This avoids the concurrency conflict (DbUpdateConcurrencyException) that occurs
/// when adding items one-by-one, since the Competition.Version is an EF concurrency token.
/// </summary>
public sealed class BatchAddBoqItemsCommandHandler
    : ICommandHandler<BatchAddBoqItemsCommand, IReadOnlyList<BoqItemDto>>
{
    private readonly ICompetitionRepository _repository;
    private readonly ILogger<BatchAddBoqItemsCommandHandler> _logger;

    public BatchAddBoqItemsCommandHandler(
        ICompetitionRepository repository,
        ILogger<BatchAddBoqItemsCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<BoqItemDto>>> Handle(
        BatchAddBoqItemsCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _repository.GetByIdWithDetailsForUpdateAsync(
            request.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<IReadOnlyList<BoqItemDto>>("Competition not found.");

        // If ClearExisting is true, remove all existing BOQ items first
        if (request.ClearExisting)
        {
            var existingIds = competition.BoqItems.Select(b => b.Id).ToList();
            foreach (var existingId in existingIds)
            {
                var removeResult = competition.RemoveBoqItem(existingId);
                if (removeResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to remove existing BOQ item {ItemId}: {Error}",
                        existingId, removeResult.Error);
                }
            }
        }

        var addedItems = new List<BoqItem>();
        var currentSortOrder = competition.BoqItems.Count;

        foreach (var input in request.Items)
        {
            currentSortOrder++;

            var item = BoqItem.Create(
                competitionId: request.CompetitionId,
                itemNumber: input.ItemNumber,
                descriptionAr: input.DescriptionAr,
                descriptionEn: input.DescriptionEn,
                unit: input.Unit,
                quantity: input.Quantity,
                estimatedUnitPrice: input.EstimatedUnitPrice,
                category: input.Category,
                createdBy: request.CreatedByUserId,
                sortOrder: currentSortOrder);

            var addResult = competition.AddBoqItem(item);
            if (addResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to add BOQ item {ItemNumber}: {Error}",
                    input.ItemNumber, addResult.Error);
                return Result.Failure<IReadOnlyList<BoqItemDto>>(addResult.Error!);
            }

            addedItems.Add(item);
        }

        // Single SaveChanges call for all items — avoids concurrency conflicts
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully added {Count} BOQ items to competition {CompetitionId}",
            addedItems.Count, request.CompetitionId);

        var dtos = addedItems
            .Select(CompetitionMapper.ToBoqItemDto)
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<BoqItemDto>>(dtos);
    }
}
