using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FeatureFlags.Dtos;

namespace TendexAI.Application.Features.FeatureFlags.Commands.ToggleFeatureFlag;

/// <summary>
/// Command to toggle a feature flag for a specific tenant.
/// Creates the flag if it doesn't exist, or updates it if it does.
/// </summary>
public sealed record ToggleFeatureFlagCommand(
    Guid TenantId,
    string FeatureKey,
    bool IsEnabled,
    string? Configuration) : ICommand<TenantFeatureFlagDto>;
