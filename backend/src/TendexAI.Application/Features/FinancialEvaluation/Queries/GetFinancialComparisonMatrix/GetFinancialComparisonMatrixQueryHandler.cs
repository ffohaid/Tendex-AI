using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Domain.Services;

namespace TendexAI.Application.Features.FinancialEvaluation.Queries.GetFinancialComparisonMatrix;

public sealed class GetFinancialComparisonMatrixQueryHandler
    : IQueryHandler<GetFinancialComparisonMatrixQuery, FinancialComparisonMatrixDto>
{
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly ISupplierOfferRepository _offerRepo;
    private readonly ICompetitionRepository _competitionRepo;

    public GetFinancialComparisonMatrixQueryHandler(
        IFinancialEvaluationRepository financialRepo,
        ISupplierOfferRepository offerRepo,
        ICompetitionRepository competitionRepo)
    {
        _financialRepo = financialRepo;
        _offerRepo = offerRepo;
        _competitionRepo = competitionRepo;
    }

    public async Task<Result<FinancialComparisonMatrixDto>> Handle(
        GetFinancialComparisonMatrixQuery request, CancellationToken cancellationToken)
    {
        var evaluation = await _financialRepo.GetFullAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<FinancialComparisonMatrixDto>("No financial evaluation found.");

        var competition = await _competitionRepo.GetByIdWithDetailsAsync(
            request.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<FinancialComparisonMatrixDto>("Competition not found.");

        var offers = await _offerRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        var passedOffers = offers
            .Where(o => o.TechnicalResult == OfferTechnicalResult.Passed && o.IsFinancialEnvelopeOpen)
            .ToList();

        var boqItems = competition.BoqItems.ToList();
        var allOfferItems = evaluation.OfferItems.ToList();

        // Use domain service to generate comparison matrix
        var matrix = FinancialScoringService.GenerateComparisonMatrix(
            passedOffers, allOfferItems, boqItems);

        var estimatedTotal = FinancialScoringService.CalculateEstimatedTotalCost(boqItems);

        // Map to DTOs
        var rowDtos = matrix.Rows.Select(r => new FinancialComparisonRowDto(
            r.BoqItemId, r.ItemNumber, r.DescriptionAr,
            r.Unit.ToString(), r.Quantity,
            r.EstimatedUnitPrice, r.EstimatedTotalPrice,
            r.SupplierPrices.Select(sp => new SupplierPriceCellDto(
                sp.OfferId, sp.BlindCode, sp.SupplierName,
                sp.UnitPrice, sp.TotalPrice,
                sp.DeviationPercentage, sp.DeviationLevel,
                sp.HasArithmeticError)).ToList().AsReadOnly()))
            .ToList().AsReadOnly();

        // Rank suppliers by total amount
        var sortedTotals = matrix.SupplierTotals
            .OrderBy(s => s.TotalAmount)
            .Select((s, index) => new SupplierTotalSummaryDto(
                s.OfferId, s.BlindCode, s.SupplierName,
                s.TotalAmount, s.DeviationPercentage,
                s.DeviationLevel, index + 1))
            .ToList().AsReadOnly();

        return Result.Success(new FinancialComparisonMatrixDto(
            request.CompetitionId, rowDtos, sortedTotals, estimatedTotal));
    }
}
