using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.SupplierOffers.Commands.DeleteSupplierOffer;

/// <summary>
/// Command to delete a supplier offer from a competition.
/// Only allowed before evaluation has started.
/// </summary>
public sealed record DeleteSupplierOfferCommand(
    Guid OfferId,
    Guid CompetitionId) : ICommand;
