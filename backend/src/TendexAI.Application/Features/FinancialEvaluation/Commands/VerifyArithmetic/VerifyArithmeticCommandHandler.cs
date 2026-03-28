using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Services;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.VerifyArithmetic;

public sealed class VerifyArithmeticCommandHandler
    : ICommandHandler<VerifyArithmeticCommand, ArithmeticVerificationResultDto>
{
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly ISupplierOfferRepository _offerRepo;
    private readonly ILogger<VerifyArithmeticCommandHandler> _logger;

    public VerifyArithmeticCommandHandler(
        IFinancialEvaluationRepository financialRepo,
        ISupplierOfferRepository offerRepo,
        ILogger<VerifyArithmeticCommandHandler> logger)
    {
        _financialRepo = financialRepo;
        _offerRepo = offerRepo;
        _logger = logger;
    }

    public async Task<Result<ArithmeticVerificationResultDto>> Handle(
        VerifyArithmeticCommand request, CancellationToken cancellationToken)
    {
        var evaluation = await _financialRepo.GetWithItemsAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<ArithmeticVerificationResultDto>(
                "No financial evaluation found.");

        var offer = await _offerRepo.GetByIdAsync(request.SupplierOfferId, cancellationToken);
        if (offer is null)
            return Result.Failure<ArithmeticVerificationResultDto>("Offer not found.");

        var offerItems = evaluation.OfferItems
            .Where(i => i.SupplierOfferId == request.SupplierOfferId)
            .ToList();

        if (offerItems.Count == 0)
            return Result.Failure<ArithmeticVerificationResultDto>(
                "No financial offer items found for this supplier.");

        int errorCount = FinancialScoringService.VerifyAllArithmetic(
            offerItems, request.VerifiedByUserId);

        var errors = offerItems
            .Where(i => i.HasArithmeticError)
            .Select(i => new ArithmeticErrorDto(
                i.BoqItemId, "",
                i.TotalPrice, i.SupplierSubmittedTotal,
                i.SupplierSubmittedTotal.HasValue
                    ? Math.Abs(i.TotalPrice - i.SupplierSubmittedTotal.Value)
                    : 0m))
            .ToList();

        _financialRepo.Update(evaluation);
        await _financialRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Arithmetic verification completed for offer {OfferId}. Errors: {ErrorCount}",
            request.SupplierOfferId, errorCount);

        return Result.Success(new ArithmeticVerificationResultDto(
            request.SupplierOfferId, offer.BlindCode,
            offerItems.Count, errorCount, errorCount > 0,
            errors.AsReadOnly()));
    }
}
