using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.SupplierOffers.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.SupplierOffers.Queries.GetSupplierOffers;

/// <summary>
/// Handler for getting all supplier offers for a competition.
/// </summary>
public sealed class GetSupplierOffersQueryHandler
    : IQueryHandler<GetSupplierOffersQuery, IReadOnlyList<SupplierOfferDto>>
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;

    public GetSupplierOffersQueryHandler(ITenantDbContextFactory tenantDbContextFactory)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
    }

    public async Task<Result<IReadOnlyList<SupplierOfferDto>>> Handle(
        GetSupplierOffersQuery request,
        CancellationToken cancellationToken)
    {
        var dbContext = _tenantDbContextFactory.CreateDbContext();
        var offers = dbContext.GetDbSet<SupplierOffer>().AsNoTracking();

        try
        {
            var dtos = await offers
                .Where(o => o.CompetitionId == request.CompetitionId && !o.IsDeleted)
                .OrderBy(o => o.BlindCode)
                .Select(MapToDto())
                .ToListAsync(cancellationToken);

            return Result.Success<IReadOnlyList<SupplierOfferDto>>(dtos);
        }
        catch (Exception ex) when (IsMissingIsDeletedColumn(ex))
        {
            var fallbackDtos = await offers
                .Where(o => o.CompetitionId == request.CompetitionId)
                .OrderBy(o => o.BlindCode)
                .Select(MapToDto())
                .ToListAsync(cancellationToken);

            return Result.Success<IReadOnlyList<SupplierOfferDto>>(fallbackDtos);
        }
    }

    private static System.Linq.Expressions.Expression<Func<SupplierOffer, SupplierOfferDto>> MapToDto()
    {
        return o => new SupplierOfferDto(
            o.Id,
            o.CompetitionId,
            o.SupplierName,
            o.SupplierIdentifier,
            o.OfferReferenceNumber,
            o.SubmissionDate,
            o.BlindCode,
            o.TechnicalResult,
            o.TechnicalTotalScore,
            o.IsFinancialEnvelopeOpen,
            o.FinancialEnvelopeOpenedAt,
            o.CreatedAt);
    }

    private static bool IsMissingIsDeletedColumn(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("Invalid column name 'IsDeleted'", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("Invalid column name \"IsDeleted\"", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("IsDeleted", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
