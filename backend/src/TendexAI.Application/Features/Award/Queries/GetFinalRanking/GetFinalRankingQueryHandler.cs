using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Award.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Domain.Services;

namespace TendexAI.Application.Features.Award.Queries.GetFinalRanking;

public sealed class GetFinalRankingQueryHandler
    : IQueryHandler<GetFinalRankingQuery, FinalRankingDto>
{
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly ISupplierOfferRepository _offerRepo;
    private readonly ICompetitionRepository _competitionRepo;

    public GetFinalRankingQueryHandler(
        IFinancialEvaluationRepository financialRepo,
        ISupplierOfferRepository offerRepo,
        ICompetitionRepository competitionRepo)
    {
        _financialRepo = financialRepo;
        _offerRepo = offerRepo;
        _competitionRepo = competitionRepo;
    }

    public async Task<Result<FinalRankingDto>> Handle(
        GetFinalRankingQuery request, CancellationToken cancellationToken)
    {
        var competition = await _competitionRepo.GetByIdAsync(
            request.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<FinalRankingDto>("Competition not found.");

        var financialEval = await _financialRepo.GetFullAsync(
            request.CompetitionId, cancellationToken);

        if (financialEval is null)
            return Result.Failure<FinalRankingDto>("No financial evaluation found.");

        var offers = await _offerRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        var passedOffers = offers
            .Where(o => o.TechnicalResult == OfferTechnicalResult.Passed)
            .ToList();

        var allOfferItems = financialEval.OfferItems.ToList();
        var boqItems = competition.BoqItems.ToList();

        // Default weights: 70% technical, 30% financial
        decimal technicalWeight = 70m;
        decimal financialWeight = 30m;

        var rankings = FinancialScoringService.GenerateFinalRanking(
            passedOffers, allOfferItems, technicalWeight, financialWeight);

        var estimatedTotal = FinancialScoringService.CalculateEstimatedTotalCost(boqItems);

        var rankingDtos = rankings.Select(r => new AwardRankingDto(
            r.OfferId, r.SupplierName, r.Rank,
            r.TechnicalScore, r.FinancialScore,
            r.CombinedScore, r.TotalOfferAmount)).ToList().AsReadOnly();

        return Result.Success(new FinalRankingDto(
            request.CompetitionId,
            competition.ProjectNameAr,
            technicalWeight, financialWeight,
            rankingDtos, estimatedTotal));
    }
}
