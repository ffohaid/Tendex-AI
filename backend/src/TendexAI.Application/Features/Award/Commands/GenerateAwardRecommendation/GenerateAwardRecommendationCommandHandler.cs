using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Award.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Domain.Services;

namespace TendexAI.Application.Features.Award.Commands.GenerateAwardRecommendation;

public sealed class GenerateAwardRecommendationCommandHandler
    : ICommandHandler<GenerateAwardRecommendationCommand, AwardRecommendationDto>
{
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly IAwardRecommendationRepository _awardRepo;
    private readonly ISupplierOfferRepository _offerRepo;
    private readonly ICompetitionRepository _competitionRepo;
    private readonly ILogger<GenerateAwardRecommendationCommandHandler> _logger;

    public GenerateAwardRecommendationCommandHandler(
        IFinancialEvaluationRepository financialRepo,
        IAwardRecommendationRepository awardRepo,
        ISupplierOfferRepository offerRepo,
        ICompetitionRepository competitionRepo,
        ILogger<GenerateAwardRecommendationCommandHandler> logger)
    {
        _financialRepo = financialRepo;
        _awardRepo = awardRepo;
        _offerRepo = offerRepo;
        _competitionRepo = competitionRepo;
        _logger = logger;
    }

    public async Task<Result<AwardRecommendationDto>> Handle(
        GenerateAwardRecommendationCommand request, CancellationToken cancellationToken)
    {
        // Validate weights
        if (request.TechnicalWeight + request.FinancialWeight != 100m)
            return Result.Failure<AwardRecommendationDto>(
                "Technical and financial weights must sum to 100.");

        // GATE: Financial evaluation must be approved
        var financialEval = await _financialRepo.GetFullAsync(
            request.CompetitionId, cancellationToken);

        if (financialEval is null)
            return Result.Failure<AwardRecommendationDto>(
                "No financial evaluation found for this competition.");

        if (!financialEval.IsReportApproved)
            return Result.Failure<AwardRecommendationDto>(
                "Financial evaluation report must be approved before generating award recommendation.");

        // Check if award already exists
        var existing = await _awardRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        if (existing is not null)
            return Result.Failure<AwardRecommendationDto>(
                "An award recommendation already exists for this competition.");

        var competition = await _competitionRepo.GetByIdAsync(
            request.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<AwardRecommendationDto>("Competition not found.");

        // Get technically-passed offers
        var offers = await _offerRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        var passedOffers = offers
            .Where(o => o.TechnicalResult == OfferTechnicalResult.Passed)
            .ToList();

        if (passedOffers.Count == 0)
            return Result.Failure<AwardRecommendationDto>(
                "No technically-passed offers found for ranking.");

        var allOfferItems = financialEval.OfferItems.ToList();

        // Generate final ranking using domain service
        var rankings = FinancialScoringService.GenerateFinalRanking(
            passedOffers, allOfferItems,
            request.TechnicalWeight, request.FinancialWeight);

        if (rankings.Count == 0)
            return Result.Failure<AwardRecommendationDto>(
                "Unable to generate ranking — no valid data.");

        var winner = rankings[0];

        // Generate justification text
        string justification = GenerateJustification(
            winner, rankings, request.TechnicalWeight, request.FinancialWeight);

        // Create award recommendation
        var award = AwardRecommendation.Create(
            request.CompetitionId,
            financialEval.TenantId,
            financialEval.Id,
            winner.OfferId,
            winner.SupplierName,
            winner.TechnicalScore,
            winner.FinancialScore,
            winner.CombinedScore,
            winner.TotalOfferAmount,
            justification,
            request.GeneratedByUserId);

        // Add all rankings
        foreach (var ranking in rankings)
        {
            var awardRanking = AwardRanking.Create(
                award.Id, ranking.OfferId, ranking.SupplierName,
                ranking.Rank, ranking.TechnicalScore, ranking.FinancialScore,
                ranking.CombinedScore, ranking.TotalOfferAmount,
                request.GeneratedByUserId);

            award.AddRanking(awardRanking);
        }

        await _awardRepo.AddAsync(award, cancellationToken);
        await _awardRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Award recommendation generated for competition {CompetitionId}. " +
            "Recommended: {SupplierName} with combined score {CombinedScore}",
            request.CompetitionId, winner.SupplierName, winner.CombinedScore);

        // Map to DTO
        var rankingDtos = rankings.Select(r => new AwardRankingDto(
            r.OfferId, r.SupplierName, r.Rank,
            r.TechnicalScore, r.FinancialScore,
            r.CombinedScore, r.TotalOfferAmount)).ToList().AsReadOnly();

        return Result.Success(new AwardRecommendationDto(
            award.Id, award.CompetitionId, award.Status,
            award.RecommendedOfferId, award.RecommendedSupplierName,
            award.TechnicalScore, award.FinancialScore,
            award.CombinedScore, award.TotalOfferAmount,
            award.Justification,
            award.ApprovedAt, award.ApprovedBy,
            award.RejectionReason,
            rankingDtos, award.CreatedAt));
    }

    private static string GenerateJustification(
        OfferRankingResult winner,
        IReadOnlyList<OfferRankingResult> rankings,
        decimal technicalWeight,
        decimal financialWeight)
    {
        var justification =
            $"بناءً على نتائج التقييم الفني (الوزن: {technicalWeight}%) " +
            $"والتقييم المالي (الوزن: {financialWeight}%)، " +
            $"حصل المورد \"{winner.SupplierName}\" على أعلى درجة مجمعة " +
            $"({winner.CombinedScore:F2}%) من بين {rankings.Count} عروض مؤهلة فنياً. " +
            $"الدرجة الفنية: {winner.TechnicalScore:F2}%، " +
            $"الدرجة المالية: {winner.FinancialScore:F2}%، " +
            $"إجمالي العرض المالي: {winner.TotalOfferAmount:N2} ريال سعودي.";

        if (rankings.Count > 1)
        {
            var secondPlace = rankings[1];
            decimal scoreDifference = winner.CombinedScore - secondPlace.CombinedScore;
            justification +=
                $" فارق الدرجة عن العرض الثاني (\"{secondPlace.SupplierName}\"): " +
                $"{scoreDifference:F2} نقطة.";
        }

        return justification;
    }
}
