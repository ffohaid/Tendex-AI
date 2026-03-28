using FluentValidation;

namespace TendexAI.Application.Features.AI.Commands.CreateAiConfiguration;

/// <summary>
/// Validates the CreateAiConfigurationCommand before processing.
/// </summary>
public sealed class CreateAiConfigurationCommandValidator
    : AbstractValidator<CreateAiConfigurationCommand>
{
    public CreateAiConfigurationCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        RuleFor(x => x.Provider)
            .IsInEnum()
            .WithMessage("Invalid AI provider.");

        RuleFor(x => x.ModelName)
            .NotEmpty()
            .MaximumLength(256)
            .WithMessage("ModelName is required and must not exceed 256 characters.");

        RuleFor(x => x.PlainApiKey)
            .NotEmpty()
            .WithMessage("API key is required.");

        RuleFor(x => x.Endpoint)
            .MaximumLength(1024)
            .When(x => x.Endpoint is not null)
            .WithMessage("Endpoint must not exceed 1024 characters.");

        RuleFor(x => x.MaxTokens)
            .GreaterThan(0)
            .LessThanOrEqualTo(128000)
            .WithMessage("MaxTokens must be between 1 and 128000.");

        RuleFor(x => x.Temperature)
            .InclusiveBetween(0.0, 2.0)
            .WithMessage("Temperature must be between 0.0 and 2.0.");
    }
}
