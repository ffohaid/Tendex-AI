using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.AddBoqItem;

/// <summary>
/// Command to add a new BOQ item to a competition.
/// </summary>
public sealed record AddBoqItemCommand(
    Guid CompetitionId,
    string ItemNumber,
    string DescriptionAr,
    string DescriptionEn,
    BoqItemUnit Unit,
    decimal Quantity,
    decimal? EstimatedUnitPrice,
    string? Category,
    string CreatedByUserId) : ICommand<BoqItemDto>;
