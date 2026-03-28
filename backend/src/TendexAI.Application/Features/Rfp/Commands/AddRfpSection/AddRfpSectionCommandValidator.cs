using FluentValidation;

namespace TendexAI.Application.Features.Rfp.Commands.AddRfpSection;

/// <summary>
/// Validates the AddRfpSectionCommand before processing.
/// </summary>
public sealed class AddRfpSectionCommandValidator : AbstractValidator<AddRfpSectionCommand>
{
    public AddRfpSectionCommandValidator()
    {
        RuleFor(x => x.CompetitionId)
            .NotEmpty()
            .WithMessage("Competition ID is required.");

        RuleFor(x => x.TitleAr)
            .NotEmpty()
            .WithMessage("Arabic section title is required.")
            .MaximumLength(500)
            .WithMessage("Arabic section title must not exceed 500 characters.");

        RuleFor(x => x.TitleEn)
            .NotEmpty()
            .WithMessage("English section title is required.")
            .MaximumLength(500)
            .WithMessage("English section title must not exceed 500 characters.");

        RuleFor(x => x.SectionType)
            .IsInEnum()
            .WithMessage("Invalid section type.");

        RuleFor(x => x.DefaultTextColor)
            .IsInEnum()
            .WithMessage("Invalid text color type.");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty()
            .WithMessage("Created by user ID is required.");
    }
}
