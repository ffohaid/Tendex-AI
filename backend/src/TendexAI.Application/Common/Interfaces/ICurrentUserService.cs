namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Provides information about the currently authenticated user.
/// Implementation resides in the Infrastructure layer (resolved from HTTP context).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Gets the unique identifier of the current user.</summary>
    Guid? UserId { get; }

    /// <summary>Gets the full name of the current user.</summary>
    string? UserName { get; }

    /// <summary>Gets the IP address of the current request.</summary>
    string? IpAddress { get; }

    /// <summary>Gets the active session identifier.</summary>
    string? SessionId { get; }

    /// <summary>Gets the tenant identifier for the current user.</summary>
    Guid? TenantId { get; }

    /// <summary>Indicates whether the current user is authenticated.</summary>
    bool IsAuthenticated { get; }
}
