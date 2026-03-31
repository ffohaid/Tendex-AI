using FluentValidation;

namespace TendexAI.Application.Features.Rfp.Commands.BatchAddBoqItems;

/// <summary>
/// Validates the BatchAddBoqItemsCommand before execution.
/// </summary>
public sealed class BatchAddBoqItemsCommandValidator : AbstractValidator<BatchAddBoqItemsCommand>
{
    public BatchAddBoqItemsCommandValidator()
    {
        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("Competition ID is required.");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one BOQ item is required.");

        RuleFor(x => x.Items.Count)
            .LessThanOrEqualTo(100)
            .WithMessage("Cannot add more than 100 BOQ items at once.");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ItemNumber)
                .NotEmpty()
                .MaximumLength(50)
                .WithMessage("Item number is required and must be 50 characters or less.");

            item.RuleFor(i => i.DescriptionAr)
                .NotEmpty()
                .MaximumLength(2000)
                .WithMessage("Arabic description is required and must be 2000 characters or less.");

            item.RuleFor(i => i.DescriptionEn)
                .NotEmpty()
                .MaximumLength(2000)
                .WithMessage("English description is required and must be 2000 characters or less.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.");

            item.RuleFor(i => i.EstimatedUnitPrice)
                .GreaterThanOrEqualTo(0)
                .When(i => i.EstimatedUnitPrice.HasValue)
                .WithMessage("Estimated unit price cannot be negative.");
        });
    }
}
