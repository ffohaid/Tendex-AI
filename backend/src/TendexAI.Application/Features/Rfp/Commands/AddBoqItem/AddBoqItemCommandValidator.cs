using FluentValidation;

namespace TendexAI.Application.Features.Rfp.Commands.AddBoqItem;

/// <summary>
/// Validates the AddBoqItemCommand before processing.
/// </summary>
public sealed class AddBoqItemCommandValidator : AbstractValidator<AddBoqItemCommand>
{
    public AddBoqItemCommandValidator()
    {
        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("Competition ID is required.");

        RuleFor(x => x.ItemNumber)
            .NotEmpty()
            .WithMessage("Item number is required.")
            .MaximumLength(50)
            .WithMessage("Item number must not exceed 50 characters.");

        RuleFor(x => x.DescriptionAr)
            .NotEmpty()
            .WithMessage("Arabic description is required.")
            .MaximumLength(2000)
            .WithMessage("Arabic description must not exceed 2000 characters.");

        RuleFor(x => x.DescriptionEn)
            .NotEmpty()
            .WithMessage("English description is required.")
            .MaximumLength(2000)
            .WithMessage("English description must not exceed 2000 characters.");

        RuleFor(x => x.Unit)
            .IsInEnum()
            .WithMessage("Invalid unit of measurement.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.EstimatedUnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Estimated unit price must be zero or greater.")
            .When(x => x.EstimatedUnitPrice.HasValue);

        RuleFor(x => x.Category)
            .MaximumLength(200)
            .WithMessage("Category must not exceed 200 characters.")
            .When(x => x.Category is not null);

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty()
            .WithMessage("Created by user ID is required.");
    }
}
