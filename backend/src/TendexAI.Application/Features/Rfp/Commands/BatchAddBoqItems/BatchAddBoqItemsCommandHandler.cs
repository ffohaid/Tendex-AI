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
        var isModifiable = await _repository.IsCompetitionModifiableAsync(
            request.CompetitionId,
            cancellationToken);

        if (!isModifiable)
        {
            var competition = await _repository.GetByIdAsync(request.CompetitionId, cancellationToken);
            if (competition is null)
                return Result.Failure<IReadOnlyList<BoqItemDto>>("Competition not found.");

            return Result.Failure<IReadOnlyList<BoqItemDto>>(
                "لا يمكن حفظ جدول الكميات: المنافسة ليست في حالة قابلة للتعديل.");
        }

        var currentBoqCount = request.ClearExisting
            ? 0
            : await _repository.GetBoqItemCountAsync(request.CompetitionId, cancellationToken);

        _logger.LogInformation(
            "Adding {Count} BOQ items to competition {CompetitionId} via direct insertion (ClearExisting={ClearExisting}, CurrentCount={CurrentCount})",
            request.Items.Count,
            request.CompetitionId,
            request.ClearExisting,
            currentBoqCount);

        var addedItems = new List<BoqItem>();
        var sortOrder = currentBoqCount;

        foreach (var input in request.Items)
        {
            sortOrder++;

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
                sortOrder: sortOrder);

            addedItems.Add(item);
        }

        try
        {
            await _repository.AddBoqItemsDirectAsync(addedItems, request.ClearExisting, cancellationToken);

            var verifiedCount = await _repository.GetBoqItemCountAsync(
                request.CompetitionId,
                cancellationToken);

            _logger.LogInformation(
                "Successfully added {Count} BOQ items to competition {CompetitionId} via direct insertion. Verified BOQ count: {VerifiedCount}",
                addedItems.Count,
                request.CompetitionId,
                verifiedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to add BOQ items to competition {CompetitionId}: {ErrorMessage}",
                request.CompetitionId,
                ex.Message);
            return Result.Failure<IReadOnlyList<BoqItemDto>>(
                $"فشل في حفظ جدول الكميات: {ex.Message}");
        }

        var dtos = addedItems
            .Select(CompetitionMapper.ToBoqItemDto)
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<BoqItemDto>>(dtos);
    }
}
