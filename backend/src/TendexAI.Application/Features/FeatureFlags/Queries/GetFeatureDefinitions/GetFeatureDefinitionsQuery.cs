using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FeatureFlags.Dtos;

namespace TendexAI.Application.Features.FeatureFlags.Queries.GetFeatureDefinitions;

/// <summary>
/// Query to retrieve all active feature definitions.
/// </summary>
public sealed record GetFeatureDefinitionsQuery() : IQuery<IReadOnlyList<FeatureDefinitionDto>>;
