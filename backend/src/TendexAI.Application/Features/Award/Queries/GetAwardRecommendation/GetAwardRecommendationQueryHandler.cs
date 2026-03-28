using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Award.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.Award.Queries.GetAwardRecommendation;

public sealed class GetAwardRecommendationQueryHandler
    : IQueryHandler<GetAwardRecommendationQuery, AwardRecommendationDto>
{
    private readonly IAwardRecommendationRepository _awardRepo;

    public GetAwardRecommendationQueryHandler(IAwardRecommendationRepository awardRepo)
    {
        _awardRepo = awardRepo;
    }

    public async Task<Result<AwardRecommendationDto>> Handle(
        GetAwardRecommendationQuery request, CancellationToken cancellationToken)
    {
        var award = await _awardRepo.GetWithRankingsAsync(
            request.CompetitionId, cancellationToken);

        if (award is null)
            return Result.Failure<AwardRecommendationDto>(
                "No award recommendation found for this competition.");

        var rankingDtos = award.Rankings
            .OrderBy(r => r.Rank)
            .Select(r => new AwardRankingDto(
                r.SupplierOfferId, r.SupplierName, r.Rank,
                r.TechnicalScore, r.FinancialScore,
                r.CombinedScore, r.TotalOfferAmount))
            .ToList().AsReadOnly();

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
}
