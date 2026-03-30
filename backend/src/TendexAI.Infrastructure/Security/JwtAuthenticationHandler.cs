using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TendexAI.Infrastructure.Security;

/// <summary>
/// Custom JWT authentication handler that validates tokens using the same RSA signing key
/// as TokenService. This replaces OpenIddict validation which required a token store in the DB.
/// Uses Microsoft.IdentityModel.JsonWebTokens for validation (available via OpenIddict dependency).
/// </summary>
public sealed class JwtAuthenticationHandler : AuthenticationHandler<JwtAuthenticationOptions>
{
    private readonly OpenIddictKeyManager _keyManager;

    public JwtAuthenticationHandler(
        IOptionsMonitor<JwtAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        OpenIddictKeyManager keyManager)
        : base(options, logger, encoder)
    {
        _keyManager = keyManager;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Extract the Authorization header
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            var signingKey = _keyManager.GetOrCreateSigningKey();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Options.Issuer,
                ValidateAudience = true,
                ValidAudience = Options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                ValidTypes = new[] { "at+jwt", "JWT" }
            };

            var handler = new JsonWebTokenHandler();
            var result = await handler.ValidateTokenAsync(token, validationParameters);

            if (!result.IsValid)
            {
                Logger.LogWarning("JWT validation failed: {Error}", result.Exception?.Message ?? "Unknown error");
                return AuthenticateResult.Fail(result.Exception ?? new SecurityTokenValidationException("Token validation failed"));
            }

            var principal = new ClaimsPrincipal(result.ClaimsIdentity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "JWT authentication error");
            return AuthenticateResult.Fail(ex);
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers["WWW-Authenticate"] = "Bearer";
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Options for the custom JWT authentication handler.
/// </summary>
public sealed class JwtAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// The expected token issuer.
    /// </summary>
    public string Issuer { get; set; } = "https://tendex-ai.com";

    /// <summary>
    /// The expected token audience.
    /// </summary>
    public string Audience { get; set; } = "tendex-ai-client";
}
