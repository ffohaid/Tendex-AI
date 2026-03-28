using TendexAI.Application.Features.Impersonation.Dtos;

namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Service interface for searching users across all tenant databases.
/// Implementation resides in Infrastructure layer to access tenant-specific DbContexts.
/// </summary>
public interface ICrossTenantUserSearchService
{
    /// <summary>
    /// Searches for users matching the given term across all (or a specific) tenant databases.
    /// </summary>
    Task<PaginatedResponse<CrossTenantUserResult>> SearchUsersAsync(
        string? searchTerm,
        Guid? tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

/// <summary>
/// Represents a user search result from cross-tenant search.
/// </summary>
public sealed record CrossTenantUserResult(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    Guid TenantId,
    string TenantName,
    bool IsActive,
    DateTime? LastLoginAt);
