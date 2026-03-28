using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FeatureFlags.Dtos;

namespace TendexAI.Application.Features.FeatureFlags.Commands.BatchToggleFeatureFlags;

/// <summary>
/// Command to batch-toggle multiple feature flags for a specific tenant.
/// Enables updating all feature flags in a single request from the operator UI.
/// </summary>
public sealed record BatchToggleFeatureFlagsCommand(
    Guid TenantId,
    IReadOnlyList<FeatureFlagToggleItem> Flags) : ICommand<IReadOnlyList<TenantFeatureFlagDto>>;

/// <summary>
/// Represents a single feature flag toggle item within a batch operation.
/// </summary>
public sealed record FeatureFlagToggleItem(
    string FeatureKey,
    bool IsEnabled,
    string? Configuration);
