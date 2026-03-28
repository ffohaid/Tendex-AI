using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Award.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.Award.Commands.ApproveAward;

public sealed class ApproveAwardCommandHandler
    : ICommandHandler<ApproveAwardCommand, AwardRecommendationDto>
{
    private readonly IAwardRecommendationRepository _awardRepo;
    private readonly ILogger<ApproveAwardCommandHandler> _logger;

    public ApproveAwardCommandHandler(
        IAwardRecommendationRepository awardRepo,
        ILogger<ApproveAwardCommandHandler> logger)
    {
        _awardRepo = awardRepo;
        _logger = logger;
    }

    public async Task<Result<AwardRecommendationDto>> Handle(
        ApproveAwardCommand request, CancellationToken cancellationToken)
    {
        var award = await _awardRepo.GetWithRankingsAsync(
            request.CompetitionId, cancellationToken);

        if (award is null)
            return Result.Failure<AwardRecommendationDto>(
                "No award recommendation found for this competition.");

        var submitResult = award.SubmitForApproval(request.ApprovedByUserId);
        if (submitResult.IsFailure)
        {
            // If already pending, try to approve directly
            var approveResult = award.Approve(request.ApprovedByUserId);
            if (approveResult.IsFailure)
                return Result.Failure<AwardRecommendationDto>(approveResult.Error!);
        }
        else
        {
            var approveResult = award.Approve(request.ApprovedByUserId);
            if (approveResult.IsFailure)
                return Result.Failure<AwardRecommendationDto>(approveResult.Error!);
        }

        _awardRepo.Update(award);
        await _awardRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Award approved for competition {CompetitionId} by {ApprovedBy}",
            request.CompetitionId, request.ApprovedByUserId);

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
