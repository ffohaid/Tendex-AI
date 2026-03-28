using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Award.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.Award.Commands.RejectAward;

public sealed class RejectAwardCommandHandler
    : ICommandHandler<RejectAwardCommand, AwardRecommendationDto>
{
    private readonly IAwardRecommendationRepository _awardRepo;
    private readonly ILogger<RejectAwardCommandHandler> _logger;

    public RejectAwardCommandHandler(
        IAwardRecommendationRepository awardRepo,
        ILogger<RejectAwardCommandHandler> logger)
    {
        _awardRepo = awardRepo;
        _logger = logger;
    }

    public async Task<Result<AwardRecommendationDto>> Handle(
        RejectAwardCommand request, CancellationToken cancellationToken)
    {
        var award = await _awardRepo.GetWithRankingsAsync(
            request.CompetitionId, cancellationToken);

        if (award is null)
            return Result.Failure<AwardRecommendationDto>(
                "No award recommendation found.");

        var rejectResult = award.Reject(request.RejectedByUserId, request.Reason);
        if (rejectResult.IsFailure)
            return Result.Failure<AwardRecommendationDto>(rejectResult.Error!);

        _awardRepo.Update(award);
        await _awardRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Award rejected for competition {CompetitionId}. Reason: {Reason}",
            request.CompetitionId, request.Reason);

        var rankingDtos = award.Rankings.Select(r => new AwardRankingDto(
            r.SupplierOfferId, r.SupplierName, r.Rank,
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
}
