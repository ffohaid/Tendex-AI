using FluentValidation;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.ReviewAiAnalysis;

/// <summary>
/// Validates the ReviewAiAnalysisCommand before processing.
/// </summary>
public sealed class ReviewAiAnalysisCommandValidator
    : AbstractValidator<ReviewAiAnalysisCommand>
{
    public ReviewAiAnalysisCommandValidator()
    {
        RuleFor(x => x.AnalysisId)
            .NotEmpty()
            .WithMessage("معرّف تحليل الذكاء الاصطناعي مطلوب.");

        RuleFor(x => x.ReviewedByUserId)
            .NotEmpty()
            .WithMessage("معرّف المراجع مطلوب.");

        RuleFor(x => x.ReviewNotes)
            .MaximumLength(4000)
            .WithMessage("ملاحظات المراجعة يجب ألا تتجاوز 4000 حرف.");
    }
}
