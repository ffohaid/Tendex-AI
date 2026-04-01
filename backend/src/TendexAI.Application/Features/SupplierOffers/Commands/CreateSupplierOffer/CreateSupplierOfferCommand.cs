using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.SupplierOffers.Dtos;

namespace TendexAI.Application.Features.SupplierOffers.Commands.CreateSupplierOffer;

/// <summary>
/// Command to create a new supplier offer for a competition.
/// </summary>
public sealed record CreateSupplierOfferCommand(
    Guid CompetitionId,
    Guid TenantId,
    string SupplierName,
    string SupplierIdentifier,
    string OfferReferenceNumber,
    DateTime SubmissionDate,
    string CreatedByUserId) : ICommand<SupplierOfferDto>;
