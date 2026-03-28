using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FeatureFlags.Dtos;

namespace TendexAI.Application.Features.FeatureFlags.Commands.CreateFeatureDefinition;

/// <summary>
/// Command to create a new global feature definition.
/// </summary>
public sealed record CreateFeatureDefinitionCommand(
    string FeatureKey,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string Category,
    bool IsEnabledByDefault) : ICommand<FeatureDefinitionDto>;
