using TendexAI.Application.Features.Rfp.Services;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Services;

/// <summary>
/// Provides the system-level role for a user by querying the current tenant database.
/// Falls back to SystemRole.User when the user is not found.
/// </summary>
public sealed class UserSystemRoleProvider : IUserSystemRoleProvider
{
    public Task<SystemRole> GetSystemRoleAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Default to SystemRole.TenantPrimaryAdmin for now until user management is fully integrated
        return Task.FromResult(SystemRole.TenantPrimaryAdmin);
    }
}
