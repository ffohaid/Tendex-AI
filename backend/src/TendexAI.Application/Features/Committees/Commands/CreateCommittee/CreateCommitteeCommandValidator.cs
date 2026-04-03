using FluentValidation;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.CreateCommittee;

/// <summary>
/// Validates the CreateCommitteeCommand input.
/// Enforces scope-type-specific rules for phases and competition links.
/// </summary>
public sealed class CreateCommitteeCommandValidator : AbstractValidator<CreateCommitteeCommand>
{
    public CreateCommitteeCommandValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("Arabic name is required.")
            .MaximumLength(200).WithMessage("Arabic name must not exceed 200 characters.");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English name is required.")
            .MaximumLength(200).WithMessage("English name must not exceed 200 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid committee type.");

        RuleFor(x => x.ScopeType)
            .IsInEnum().WithMessage("Invalid scope type.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        // Phase range required for non-Comprehensive scopes
        RuleFor(x => x.ActiveFromPhase)
            .NotNull()
            .When(x => x.ScopeType != CommitteeScopeType.Comprehensive)
            .WithMessage("Active-from phase is required for the selected scope type.");

        RuleFor(x => x.ActiveToPhase)
            .NotNull()
            .When(x => x.ScopeType != CommitteeScopeType.Comprehensive)
            .WithMessage("Active-to phase is required for the selected scope type.");

        // Phase range must not be set for Comprehensive scope
        RuleFor(x => x.ActiveFromPhase)
            .Null()
            .When(x => x.ScopeType == CommitteeScopeType.Comprehensive)
            .WithMessage("Comprehensive scope committees cannot have phase restrictions.");

        RuleFor(x => x.ActiveToPhase)
            .Null()
            .When(x => x.ScopeType == CommitteeScopeType.Comprehensive)
            .WithMessage("Comprehensive scope committees cannot have phase restrictions.");

        // Competition IDs required for SpecificPhasesSpecificCompetitions
        RuleFor(x => x.CompetitionIds)
            .Must(ids => ids is { Count: > 0 })
            .When(x => x.ScopeType == CommitteeScopeType.SpecificPhasesSpecificCompetitions)
            .WithMessage("At least one competition must be linked for the selected scope type.");

        // Competition IDs must not be set for other scopes
        RuleFor(x => x.CompetitionIds)
            .Must(ids => ids is null or { Count: 0 })
            .When(x => x.ScopeType != CommitteeScopeType.SpecificPhasesSpecificCompetitions)
            .WithMessage("Competitions can only be linked when scope is 'Specific Phases - Specific Competitions'.");
    }
}
