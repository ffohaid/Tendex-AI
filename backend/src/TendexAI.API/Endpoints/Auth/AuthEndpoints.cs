using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Auth.Commands;
using TendexAI.Application.Features.Auth.Dtos;

namespace TendexAI.API.Endpoints.Auth;

/// <summary>
/// Defines Minimal API endpoints for authentication operations.
/// All endpoints follow RESTful conventions and return standardized responses.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps all authentication-related endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Authenticate user with email and password")
            .AllowAnonymous()
            .Produces<AuthTokenResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh-token", RefreshTokenAsync)
            .WithName("RefreshToken")
            .WithSummary("Refresh an expired access token")
            .AllowAnonymous()
            .Produces<AuthTokenResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .WithSummary("Log out and revoke tokens")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        group.MapPost("/mfa/verify", VerifyMfaAsync)
            .WithName("VerifyMfa")
            .WithSummary("Verify MFA code during login")
            .AllowAnonymous()
            .Produces<AuthTokenResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/mfa/setup", SetupMfaAsync)
            .WithName("SetupMfa")
            .WithSummary("Enable MFA for the authenticated user")
            .RequireAuthorization()
            .Produces<MfaSetupResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/mfa/disable", DisableMfaAsync)
            .WithName("DisableMfa")
            .WithSummary("Disable MFA for the authenticated user")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .WithName("ForgotPassword")
            .WithSummary("Request a password reset email")
            .WithDescription("Sends a password reset link to the user's registered email address. Always returns 200 OK to prevent email enumeration.")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/reset-password", ResetPasswordAsync)
            .WithName("ResetPassword")
            .WithSummary("Reset password using a valid reset token")
            .WithDescription("Resets the user's password using the token received via email. Revokes all existing sessions after successful reset.")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }

    /// <summary>
    /// POST /api/v1/auth/login
    /// Authenticates a user with email and password.
    /// </summary>
    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        // Inject the tenant ID from the request body into the HTTP header
        // so that TenantProvider can resolve the tenant connection string.
        // This is necessary because TenantDbContext is resolved via DI
        // before the command handler runs, and TenantProvider reads from headers.
        if (request.TenantId != Guid.Empty)
        {
            httpContext.Request.Headers["X-Tenant-Id"] = request.TenantId.ToString();
        }

        var ipAddress = GetIpAddress(httpContext);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var command = new LoginCommand(
            request.Email,
            request.Password,
            ipAddress,
            userAgent,
            request.TenantId);

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed");
        }

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// POST /api/v1/auth/refresh-token
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    private static async Task<IResult> RefreshTokenAsync(
        [FromBody] RefreshTokenRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        // Inject tenant ID into header for TenantProvider resolution
        if (request.TenantId != Guid.Empty)
        {
            httpContext.Request.Headers["X-Tenant-Id"] = request.TenantId.ToString();
        }

        var ipAddress = GetIpAddress(httpContext);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var command = new RefreshTokenCommand(
            request.RefreshToken,
            ipAddress,
            userAgent,
            request.TenantId);

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Token Refresh Failed");
        }

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// POST /api/v1/auth/logout
    /// Logs out the authenticated user by revoking tokens and sessions.
    /// </summary>
    private static async Task<IResult> LogoutAsync(
        [FromBody] LogoutRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
        {
            return Results.Problem(
                detail: "User not authenticated.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");
        }

        var tenantId = GetTenantId(httpContext);
        var ipAddress = GetIpAddress(httpContext);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var command = new LogoutCommand(
            userId.Value,
            request.RefreshToken,
            request.SessionId,
            ipAddress,
            userAgent,
            tenantId);

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Logout Failed");
        }

        return Results.NoContent();
    }

    /// <summary>
    /// POST /api/v1/auth/mfa/verify
    /// Verifies a TOTP code during the MFA login flow.
    /// </summary>
    private static async Task<IResult> VerifyMfaAsync(
        [FromBody] VerifyMfaRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        // Inject tenant ID into header for TenantProvider resolution
        if (request.TenantId != Guid.Empty)
        {
            httpContext.Request.Headers["X-Tenant-Id"] = request.TenantId.ToString();
        }

        var ipAddress = GetIpAddress(httpContext);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var command = new VerifyMfaCommand(
            request.SessionId,
            request.Code,
            ipAddress,
            userAgent,
            request.TenantId);

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "MFA Verification Failed");
        }

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// POST /api/v1/auth/mfa/setup
    /// Initiates MFA setup for the authenticated user.
    /// </summary>
    private static async Task<IResult> SetupMfaAsync(
        [FromBody] SetupMfaRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
        {
            return Results.Problem(
                detail: "User not authenticated.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");
        }

        var ipAddress = GetIpAddress(httpContext);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var command = new SetupMfaCommand(
            userId.Value,
            ipAddress,
            userAgent,
            request.TenantId);

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "MFA Setup Failed");
        }

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// POST /api/v1/auth/mfa/disable
    /// Disables MFA for the authenticated user.
    /// </summary>
    private static async Task<IResult> DisableMfaAsync(
        [FromBody] DisableMfaRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
        {
            return Results.Problem(
                detail: "User not authenticated.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");
        }

        var ipAddress = GetIpAddress(httpContext);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var command = new DisableMfaCommand(
            userId.Value,
            request.Code,
            ipAddress,
            userAgent,
            request.TenantId);

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "MFA Disable Failed");
        }

        return Results.NoContent();
    }

    /// <summary>
    /// POST /api/v1/auth/forgot-password
    /// Initiates the password reset flow by sending a reset email.
    /// Always returns 200 OK to prevent email enumeration attacks.
    /// </summary>
    private static async Task<IResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        // Inject tenant ID into header for TenantProvider resolution
        if (request.TenantId != Guid.Empty)
        {
            httpContext.Request.Headers["X-Tenant-Id"] = request.TenantId.ToString();
        }

        var ipAddress = GetIpAddress(httpContext);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var command = new ForgotPasswordCommand(
            request.Email,
            request.TenantId,
            ipAddress,
            userAgent);

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Forgot Password Failed");
        }

        return Results.Ok(new { Message = "إذا كان البريد الإلكتروني مسجلاً لدينا، فسيتم إرسال رابط إعادة تعيين كلمة المرور." });
    }

    /// <summary>
    /// POST /api/v1/auth/reset-password
    /// Resets the user's password using a valid reset token.
    /// </summary>
    private static async Task<IResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        // Inject tenant ID into header for TenantProvider resolution
        if (request.TenantId != Guid.Empty)
        {
            httpContext.Request.Headers["X-Tenant-Id"] = request.TenantId.ToString();
        }

        var ipAddress = GetIpAddress(httpContext);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var command = new ResetPasswordCommand(
            request.SessionId,
            request.Token,
            request.NewPassword,
            request.ConfirmPassword,
            request.TenantId,
            ipAddress,
            userAgent);

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Password Reset Failed");
        }

        return Results.Ok(new { Message = "تم إعادة تعيين كلمة المرور بنجاح. يرجى تسجيل الدخول بكلمة المرور الجديدة." });
    }

    // ----- Helper Methods -----

    private static string GetIpAddress(HttpContext context)
    {
        // Check for forwarded headers (behind reverse proxy)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var subClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? context.User.FindFirst("sub")?.Value;

        return subClaim is not null && Guid.TryParse(subClaim, out var userId) ? userId : null;
    }

    private static Guid GetTenantId(HttpContext context)
    {
        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
        return tenantClaim is not null && Guid.TryParse(tenantClaim, out var tenantId)
            ? tenantId
            : Guid.Empty;
    }
}
