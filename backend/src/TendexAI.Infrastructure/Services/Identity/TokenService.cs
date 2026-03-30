using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Infrastructure.Security;

namespace TendexAI.Infrastructure.Services.Identity;

/// <summary>
/// Service for generating and validating JWT access tokens and refresh tokens.
/// Uses the same RSA signing key as OpenIddict to ensure token validation consistency.
/// Access Token validity: 60 minutes. Refresh Token validity: 8 hours.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly OpenIddictKeyManager _keyManager;

    public TokenService(IConfiguration configuration, OpenIddictKeyManager keyManager)
    {
        _configuration = configuration;
        _keyManager = keyManager;
    }

    /// <inheritdoc />
    public string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles, IEnumerable<string> permissions, Guid tenantId)
    {
        var signingKey = _keyManager.GetOrCreateSigningKey();
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new("tenant_id", tenantId.ToString()),
            new("first_name", user.FirstName),
            new("last_name", user.LastName),
            new("security_stamp", user.SecurityStamp),
            new("mfa_enabled", user.MfaEnabled.ToString().ToLowerInvariant())
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permission claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var issuer = _configuration["Authentication:Issuer"] ?? "https://tendex-ai.com";
        var audience = _configuration["Authentication:Audience"] ?? "tendex-ai-client";

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(60), // 60-minute access token
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials,
            TokenType = "at+jwt"
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        var signingKey = _keyManager.GetOrCreateSigningKey();
        var issuer = _configuration["Authentication:Issuer"] ?? "https://tendex-ai.com";
        var audience = _configuration["Authentication:Audience"] ?? "tendex-ai-client";

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = false, // Allow expired tokens for refresh flow
            ClockSkew = TimeSpan.Zero
        };

        var handler = new JsonWebTokenHandler();
        var result = handler.ValidateTokenAsync(token, validationParameters).GetAwaiter().GetResult();

        return result.IsValid ? new ClaimsPrincipal(result.ClaimsIdentity) : null;
    }

    /// <inheritdoc />
    public Guid? GetUserIdFromToken(string token)
    {
        var principal = ValidateAccessToken(token);
        var subClaim = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                       ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return subClaim is not null && Guid.TryParse(subClaim, out var userId) ? userId : null;
    }
}
