using FluentValidation;

namespace TendexAI.Application.Features.AI.Commands.GenerateBookletStructure;

/// <summary>
/// Validates the GenerateBookletStructureCommand before processing.
/// </summary>
public sealed class GenerateBookletStructureCommandValidator
    : AbstractValidator<GenerateBookletStructureCommand>
{
    public GenerateBookletStructureCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("CompetitionId is required.");

        RuleFor(x => x.ProjectNameAr)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("ProjectNameAr is required and must not exceed 500 characters.");

        RuleFor(x => x.ProjectDescriptionAr)
            .NotEmpty()
            .MaximumLength(5000)
            .WithMessage("ProjectDescriptionAr is required and must not exceed 5000 characters.");

        RuleFor(x => x.ProjectType)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("ProjectType is required and must not exceed 200 characters.");

        RuleFor(x => x.EstimatedBudget)
            .GreaterThan(0)
            .When(x => x.EstimatedBudget.HasValue)
            .WithMessage("EstimatedBudget must be greater than 0 when provided.");

        RuleFor(x => x.CollectionName)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("CollectionName is required and must not exceed 200 characters.");
    }
}
