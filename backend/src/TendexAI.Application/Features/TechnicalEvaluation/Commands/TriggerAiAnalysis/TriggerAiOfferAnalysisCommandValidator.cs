using FluentValidation;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.TriggerAiAnalysis;

/// <summary>
/// Validates the TriggerAiOfferAnalysisCommand before processing.
/// </summary>
public sealed class TriggerAiOfferAnalysisCommandValidator
    : AbstractValidator<TriggerAiOfferAnalysisCommand>
{
    public TriggerAiOfferAnalysisCommandValidator()
    {
        RuleFor(x => x.EvaluationId)
            .NotEmpty()
            .WithMessage("معرّف التقييم الفني مطلوب.");

        RuleFor(x => x.TriggeredByUserId)
            .NotEmpty()
            .WithMessage("معرّف المستخدم الذي بدأ التحليل مطلوب.");
    }
}
