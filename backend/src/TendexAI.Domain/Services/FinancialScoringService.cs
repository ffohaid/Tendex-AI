using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Services;

/// <summary>
/// Domain service for financial evaluation calculations.
/// Per PRD Sections 10.2, 10.3, and 11.
/// </summary>
public static class FinancialScoringService
{
    public const decimal WithinRangeThreshold = 10m;
    public const decimal ModerateDeviationThreshold = 25m;

    public static decimal CalculateTotalOfferAmount(IReadOnlyList<FinancialOfferItem> offerItems)
    {
        return offerItems.Sum(item => item.TotalPrice);
    }

    public static decimal CalculateEstimatedTotalCost(IReadOnlyList<BoqItem> boqItems)
    {
        return boqItems
            .Where(b => b.EstimatedTotalPrice.HasValue)
            .Sum(b => b.EstimatedTotalPrice!.Value);
    }

    public static int VerifyAllArithmetic(
        IReadOnlyList<FinancialOfferItem> offerItems, string verifiedBy)
    {
        int errorCount = 0;
        foreach (var item in offerItems)
        {
            item.VerifyArithmetic(item.SupplierSubmittedTotal, verifiedBy);
            if (item.HasArithmeticError) errorCount++;
        }
        return errorCount;
    }

    public static void CalculateDeviations(
        IReadOnlyList<FinancialOfferItem> offerItems,
        IReadOnlyList<BoqItem> boqItems, string modifiedBy)
    {
        foreach (var item in offerItems)
        {
            var boqItem = boqItems.FirstOrDefault(b => b.Id == item.BoqItemId);
            if (boqItem is not null)
                item.CalculateDeviation(boqItem.EstimatedUnitPrice, modifiedBy);
        }
    }

    public static PriceDeviationLevel ClassifyDeviation(decimal deviationPercentage)
    {
        decimal absDeviation = Math.Abs(deviationPercentage);
        return absDeviation switch
        {
            <= WithinRangeThreshold => PriceDeviationLevel.WithinRange,
            <= ModerateDeviationThreshold => PriceDeviationLevel.ModerateDeviation,
            _ => PriceDeviationLevel.SignificantDeviation
        };
    }

    public static decimal CalculateFinancialScore(
        decimal supplierTotalAmount, decimal lowestTotalAmount)
    {
        if (supplierTotalAmount <= 0 || lowestTotalAmount <= 0) return 0m;
        return Math.Round((lowestTotalAmount / supplierTotalAmount) * 100m, 2);
    }

    public static decimal CalculateCombinedScore(
        decimal technicalScore, decimal financialScore,
        decimal technicalWeight = 70m, decimal financialWeight = 30m)
    {
        return Math.Round(
            (technicalScore * technicalWeight / 100m) +
            (financialScore * financialWeight / 100m), 2);
    }

    public static IReadOnlyList<OfferRankingResult> GenerateFinalRanking(
        IReadOnlyList<SupplierOffer> passedOffers,
        IReadOnlyList<FinancialOfferItem> allOfferItems,
        decimal technicalWeight = 70m, decimal financialWeight = 30m)
    {
        var offerTotals = passedOffers.Select(offer =>
        {
            var items = allOfferItems.Where(i => i.SupplierOfferId == offer.Id).ToList();
            return new { Offer = offer, TotalAmount = CalculateTotalOfferAmount(items) };
        }).ToList();

        decimal lowestTotal = offerTotals.Count > 0
            ? offerTotals.Min(o => o.TotalAmount)
            : 0m;

        var scoredOffers = offerTotals.Select(o =>
        {
            decimal financialScore = CalculateFinancialScore(o.TotalAmount, lowestTotal);
            decimal technicalScore = o.Offer.TechnicalTotalScore ?? 0m;
            decimal combinedScore = CalculateCombinedScore(
                technicalScore, financialScore, technicalWeight, financialWeight);

            return new OfferRankingResult(
                o.Offer.Id,
                o.Offer.SupplierName,
                o.Offer.BlindCode,
                technicalScore,
                financialScore,
                combinedScore,
                o.TotalAmount,
                0); // Rank will be assigned below
        })
        .OrderByDescending(o => o.CombinedScore)
        .ThenBy(o => o.TotalOfferAmount)
        .ToList();

        // Assign ranks
        var rankedResults = new List<OfferRankingResult>();
        for (int i = 0; i < scoredOffers.Count; i++)
        {
            rankedResults.Add(scoredOffers[i] with { Rank = i + 1 });
        }

        return rankedResults.AsReadOnly();
    }

    public static FinancialComparisonMatrix GenerateComparisonMatrix(
        IReadOnlyList<SupplierOffer> offers,
        IReadOnlyList<FinancialOfferItem> allOfferItems,
        IReadOnlyList<BoqItem> boqItems)
    {
        var rows = new List<FinancialComparisonRow>();

        foreach (var boqItem in boqItems)
        {
            var supplierPrices = new List<SupplierPriceCell>();

            foreach (var offer in offers)
            {
                var offerItem = allOfferItems.FirstOrDefault(
                    i => i.SupplierOfferId == offer.Id && i.BoqItemId == boqItem.Id);

                if (offerItem is not null)
                {
                    supplierPrices.Add(new SupplierPriceCell(
                        offer.Id,
                        offer.BlindCode,
                        offer.SupplierName,
                        offerItem.UnitPrice,
                        offerItem.TotalPrice,
                        offerItem.DeviationPercentage ?? 0m,
                        offerItem.DeviationLevel ?? PriceDeviationLevel.WithinRange,
                        offerItem.HasArithmeticError));
                }
            }

            rows.Add(new FinancialComparisonRow(
                boqItem.Id,
                boqItem.ItemNumber,
                boqItem.DescriptionAr,
                boqItem.Unit,
                boqItem.Quantity,
                boqItem.EstimatedUnitPrice,
                boqItem.EstimatedTotalPrice,
                supplierPrices.AsReadOnly()));
        }

        // Calculate totals per supplier
        var supplierTotals = offers.Select(offer =>
        {
            var items = allOfferItems.Where(i => i.SupplierOfferId == offer.Id).ToList();
            decimal total = CalculateTotalOfferAmount(items);
            decimal estimatedTotal = CalculateEstimatedTotalCost(boqItems);
            decimal deviationPct = estimatedTotal > 0
                ? ((total - estimatedTotal) / estimatedTotal) * 100m
                : 0m;

            return new SupplierTotalSummary(
                offer.Id, offer.BlindCode, offer.SupplierName,
                total, Math.Round(deviationPct, 2),
                ClassifyDeviation(deviationPct));
        }).ToList();

        return new FinancialComparisonMatrix(rows.AsReadOnly(), supplierTotals.AsReadOnly());
    }
}

// ═══════════════════════════════════════════════════════════
//  Value Objects / Records for Financial Scoring
// ═══════════════════════════════════════════════════════════

public sealed record OfferRankingResult(
    Guid OfferId,
    string SupplierName,
    string BlindCode,
    decimal TechnicalScore,
    decimal FinancialScore,
    decimal CombinedScore,
    decimal TotalOfferAmount,
    int Rank);

public sealed record FinancialComparisonMatrix(
    IReadOnlyList<FinancialComparisonRow> Rows,
    IReadOnlyList<SupplierTotalSummary> SupplierTotals);

public sealed record FinancialComparisonRow(
    Guid BoqItemId,
    string ItemNumber,
    string DescriptionAr,
    BoqItemUnit Unit,
    decimal Quantity,
    decimal? EstimatedUnitPrice,
    decimal? EstimatedTotalPrice,
    IReadOnlyList<SupplierPriceCell> SupplierPrices);

public sealed record SupplierPriceCell(
    Guid OfferId,
    string BlindCode,
    string SupplierName,
    decimal UnitPrice,
    decimal TotalPrice,
    decimal DeviationPercentage,
    PriceDeviationLevel DeviationLevel,
    bool HasArithmeticError);

public sealed record SupplierTotalSummary(
    Guid OfferId,
    string BlindCode,
    string SupplierName,
    decimal TotalAmount,
    decimal DeviationPercentage,
    PriceDeviationLevel DeviationLevel);
