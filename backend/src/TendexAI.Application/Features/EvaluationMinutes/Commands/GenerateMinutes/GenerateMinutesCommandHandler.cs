using System.Text.Json;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.EvaluationMinutes.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Domain.Services;

namespace TendexAI.Application.Features.EvaluationMinutes.Commands.GenerateMinutes;

public sealed class GenerateMinutesCommandHandler
    : ICommandHandler<GenerateMinutesCommand, EvaluationMinutesDto>
{
    private readonly ICompetitionRepository _competitionRepo;
    private readonly ITechnicalEvaluationRepository _technicalRepo;
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly IAwardRecommendationRepository _awardRepo;
    private readonly ISupplierOfferRepository _offerRepo;
    private readonly IEvaluationMinutesRepository _minutesRepo;
    private readonly ILogger<GenerateMinutesCommandHandler> _logger;

    public GenerateMinutesCommandHandler(
        ICompetitionRepository competitionRepo,
        ITechnicalEvaluationRepository technicalRepo,
        IFinancialEvaluationRepository financialRepo,
        IAwardRecommendationRepository awardRepo,
        ISupplierOfferRepository offerRepo,
        IEvaluationMinutesRepository minutesRepo,
        ILogger<GenerateMinutesCommandHandler> logger)
    {
        _competitionRepo = competitionRepo;
        _technicalRepo = technicalRepo;
        _financialRepo = financialRepo;
        _awardRepo = awardRepo;
        _offerRepo = offerRepo;
        _minutesRepo = minutesRepo;
        _logger = logger;
    }

    public async Task<Result<EvaluationMinutesDto>> Handle(
        GenerateMinutesCommand request, CancellationToken cancellationToken)
    {
        var competition = await _competitionRepo.GetByIdAsync(
            request.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<EvaluationMinutesDto>("Competition not found.");

        var offers = await _offerRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        string titleAr;
        string contentJson;

        switch (request.MinutesType)
        {
            case MinutesType.TechnicalEvaluation:
                var techResult = await GenerateTechnicalMinutes(
                    competition, offers, cancellationToken);
                if (techResult.IsFailure)
                    return Result.Failure<EvaluationMinutesDto>(techResult.Error!);
                (titleAr, contentJson) = techResult.Value;
                break;

            case MinutesType.FinancialEvaluation:
                var finResult = await GenerateFinancialMinutes(
                    competition, offers, cancellationToken);
                if (finResult.IsFailure)
                    return Result.Failure<EvaluationMinutesDto>(finResult.Error!);
                (titleAr, contentJson) = finResult.Value;
                break;

            case MinutesType.FinalComprehensive:
                var compResult = await GenerateFinalComprehensiveMinutes(
                    competition, offers, cancellationToken);
                if (compResult.IsFailure)
                    return Result.Failure<EvaluationMinutesDto>(compResult.Error!);
                (titleAr, contentJson) = compResult.Value;
                break;

            default:
                return Result.Failure<EvaluationMinutesDto>("Invalid minutes type.");
        }

        var minutes = Domain.Entities.Evaluation.EvaluationMinutes.Create(
            request.CompetitionId, competition.TenantId,
            request.MinutesType, titleAr, contentJson,
            request.GeneratedByUserId);

        await _minutesRepo.AddAsync(minutes, cancellationToken);
        await _minutesRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Evaluation minutes generated for competition {CompetitionId}, type: {MinutesType}",
            request.CompetitionId, request.MinutesType);

        return Result.Success(new EvaluationMinutesDto(
            minutes.Id, minutes.CompetitionId, minutes.MinutesType,
            minutes.TitleAr, minutes.Status,
            minutes.ApprovedAt, minutes.ApprovedBy,
            minutes.RejectionReason, minutes.PdfFileUrl,
            [], minutes.CreatedAt));
    }

    private async Task<Result<(string TitleAr, string ContentJson)>> GenerateTechnicalMinutes(
        Competition competition,
        IReadOnlyList<SupplierOffer> offers,
        CancellationToken cancellationToken)
    {
        var technicalEval = await _technicalRepo.GetByCompetitionIdAsync(
            competition.Id, cancellationToken);

        if (technicalEval is null)
            return Result.Failure<(string, string)>("Technical evaluation not found.");

        string titleAr = $"محضر فحص العروض الفنية - {competition.ProjectNameAr}";

        var content = new
        {
            CompetitionName = competition.ProjectNameAr,
            CompetitionNumber = competition.ReferenceNumber,
            MinutesDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            EvaluationType = "فني",
            TotalOffers = offers.Count,
            PassedOffers = offers.Count(o => o.TechnicalResult == OfferTechnicalResult.Passed),
            FailedOffers = offers.Count(o => o.TechnicalResult == OfferTechnicalResult.Failed),
            MinimumPassingScore = technicalEval.MinimumPassingScore,
            OfferResults = offers.Select(o => new
            {
                SupplierName = o.SupplierName,
                BlindCode = o.BlindCode,
                TechnicalScore = o.TechnicalTotalScore ?? 0m,
                Result = o.TechnicalResult.ToString(),
                IsFinancialEnvelopeOpen = o.IsFinancialEnvelopeOpen
            }).OrderByDescending(o => o.TechnicalScore).ToList(),
            Recommendation = offers.Any(o => o.TechnicalResult == OfferTechnicalResult.Passed)
                ? "توصي اللجنة بفتح المظاريف المالية للعروض المؤهلة فنياً."
                : "لم يتأهل أي عرض فنياً. توصي اللجنة بإعادة طرح المنافسة.",
            EvaluationStartedAt = technicalEval.StartedAt,
            EvaluationCompletedAt = technicalEval.CompletedAt,
            ApprovedAt = technicalEval.ApprovedAt,
            ApprovedBy = technicalEval.ApprovedBy
        };

        return Result.Success((titleAr, JsonSerializer.Serialize(content,
            new JsonSerializerOptions { WriteIndented = true })));
    }

    private async Task<Result<(string TitleAr, string ContentJson)>> GenerateFinancialMinutes(
        Competition competition,
        IReadOnlyList<SupplierOffer> offers,
        CancellationToken cancellationToken)
    {
        var financialEval = await _financialRepo.GetFullAsync(
            competition.Id, cancellationToken);

        if (financialEval is null)
            return Result.Failure<(string, string)>("Financial evaluation not found.");

        var passedOffers = offers
            .Where(o => o.TechnicalResult == OfferTechnicalResult.Passed)
            .ToList();

        var boqItems = competition.BoqItems.ToList();
        var allOfferItems = financialEval.OfferItems.ToList();

        string titleAr = $"محضر فحص العروض المالية - {competition.ProjectNameAr}";

        // Calculate totals per offer
        var offerSummaries = passedOffers.Select(offer =>
        {
            var items = allOfferItems.Where(i => i.SupplierOfferId == offer.Id).ToList();
            decimal total = FinancialScoringService.CalculateTotalOfferAmount(items);
            int arithmeticErrors = items.Count(i => i.HasArithmeticError);

            return new
            {
                SupplierName = offer.SupplierName,
                BlindCode = offer.BlindCode,
                TotalAmount = total,
                ArithmeticErrors = arithmeticErrors,
                ItemCount = items.Count
            };
        }).OrderBy(o => o.TotalAmount).ToList();

        var estimatedTotal = FinancialScoringService.CalculateEstimatedTotalCost(boqItems);

        var content = new
        {
            CompetitionName = competition.ProjectNameAr,
            CompetitionNumber = competition.ReferenceNumber,
            MinutesDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            EvaluationType = "مالي",
            EstimatedTotalCost = estimatedTotal,
            TotalEvaluatedOffers = passedOffers.Count,
            OfferSummaries = offerSummaries,
            LowestOffer = offerSummaries.FirstOrDefault(),
            HighestOffer = offerSummaries.LastOrDefault(),
            EvaluationStartedAt = financialEval.StartedAt,
            EvaluationCompletedAt = financialEval.CompletedAt,
            ApprovedAt = financialEval.ApprovedAt,
            ApprovedBy = financialEval.ApprovedBy,
            Recommendation = "توصي اللجنة باعتماد نتائج التقييم المالي والانتقال لمرحلة الترسية."
        };

        return Result.Success((titleAr, JsonSerializer.Serialize(content,
            new JsonSerializerOptions { WriteIndented = true })));
    }

    private async Task<Result<(string TitleAr, string ContentJson)>> GenerateFinalComprehensiveMinutes(
        Competition competition,
        IReadOnlyList<SupplierOffer> offers,
        CancellationToken cancellationToken)
    {
        var financialEval = await _financialRepo.GetFullAsync(
            competition.Id, cancellationToken);

        if (financialEval is null)
            return Result.Failure<(string, string)>("Financial evaluation not found.");

        if (!financialEval.IsReportApproved)
            return Result.Failure<(string, string)>(
                "Financial evaluation must be approved before generating final minutes.");

        var award = await _awardRepo.GetWithRankingsAsync(
            competition.Id, cancellationToken);

        var passedOffers = offers
            .Where(o => o.TechnicalResult == OfferTechnicalResult.Passed)
            .ToList();

        var allOfferItems = financialEval.OfferItems.ToList();
        var boqItems = competition.BoqItems.ToList();

        // Generate final ranking
        var rankings = FinancialScoringService.GenerateFinalRanking(
            passedOffers, allOfferItems);

        var estimatedTotal = FinancialScoringService.CalculateEstimatedTotalCost(boqItems);

        string titleAr = $"المحضر النهائي الشامل - {competition.ProjectNameAr}";

        var content = new
        {
            CompetitionName = competition.ProjectNameAr,
            CompetitionNumber = competition.ReferenceNumber,
            MinutesDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            EvaluationType = "شامل",
            EstimatedTotalCost = estimatedTotal,
            TotalSubmittedOffers = offers.Count,
            TechnicallyPassedOffers = passedOffers.Count,
            TechnicallyFailedOffers = offers.Count - passedOffers.Count,
            FinalRankings = rankings.Select(r => new
            {
                Rank = r.Rank,
                SupplierName = r.SupplierName,
                TechnicalScore = r.TechnicalScore,
                FinancialScore = r.FinancialScore,
                CombinedScore = r.CombinedScore,
                TotalOfferAmount = r.TotalOfferAmount,
                DeviationFromEstimate = estimatedTotal > 0
                    ? Math.Round(((r.TotalOfferAmount - estimatedTotal) / estimatedTotal) * 100m, 2)
                    : 0m
            }).ToList(),
            RecommendedSupplier = rankings.FirstOrDefault()?.SupplierName,
            RecommendedAmount = rankings.FirstOrDefault()?.TotalOfferAmount ?? 0m,
            AwardStatus = award?.Status.ToString() ?? "NotGenerated",
            AwardJustification = award?.Justification,
            Recommendation = rankings.Count > 0
                ? $"توصي اللجنة بترسية المنافسة على المورد \"{rankings[0].SupplierName}\" " +
                  $"بمبلغ إجمالي قدره {rankings[0].TotalOfferAmount:N2} ريال سعودي، " +
                  $"حيث حصل على أعلى درجة مجمعة ({rankings[0].CombinedScore:F2}%)."
                : "لا توجد عروض مؤهلة للترسية."
        };

        return Result.Success((titleAr, JsonSerializer.Serialize(content,
            new JsonSerializerOptions { WriteIndented = true })));
    }
}
