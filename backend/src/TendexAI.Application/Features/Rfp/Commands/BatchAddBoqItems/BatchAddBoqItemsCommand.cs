using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.BatchAddBoqItems;

/// <summary>
/// Command to add multiple BOQ items to a competition in a single transaction.
/// Prevents concurrency conflicts that occur when adding items one by one.
/// </summary>
public sealed record BatchAddBoqItemsCommand(
    Guid CompetitionId,
    IReadOnlyList<BatchBoqItemInput> Items,
    bool ClearExisting,
    string CreatedByUserId) : ICommand<IReadOnlyList<BoqItemDto>>;

/// <summary>
/// Input DTO for a single BOQ item in a batch operation.
/// </summary>
public sealed record BatchBoqItemInput(
    string ItemNumber,
    string DescriptionAr,
    string DescriptionEn,
    BoqItemUnit Unit,
    decimal Quantity,
    decimal? EstimatedUnitPrice,
    string? Category);
