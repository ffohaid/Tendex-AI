using System.Security.Claims;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Common.Interfaces.Identity;

/// <summary>
/// Abstraction for token generation and validation operations.
/// </summary>
public interface ITokenService
{
    /// <summary>Generates a JWT access token for the specified user (60-minute validity).</summary>
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles, IEnumerable<string> permissions, Guid tenantId);

    /// <summary>Generates a cryptographically secure refresh token string.</summary>
    string GenerateRefreshToken();

    /// <summary>Validates an access token and returns the claims principal.</summary>
    ClaimsPrincipal? ValidateAccessToken(string token);

    /// <summary>Extracts the user ID from a JWT access token.</summary>
    Guid? GetUserIdFromToken(string token);
}
