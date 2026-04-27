using MediatR;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Features.Tenants.Commands.ChangeTenantStatus;
using TendexAI.Application.Features.Tenants.Commands.CreateTenant;
using TendexAI.Application.Features.Tenants.Commands.OperatorResetTenantAdminPassword;
using TendexAI.Application.Features.Tenants.Commands.SetupTenantAdmin;
using TendexAI.Application.Features.Tenants.Commands.ProvisionTenantDatabase;
using TendexAI.Application.Features.Tenants.Commands.UpdateTenant;
using TendexAI.Application.Features.Tenants.Commands.UpdateTenantBranding;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Application.Features.Tenants.Queries.GetTenantBranding;
using TendexAI.Application.Features.Tenants.Queries.GetTenantById;
using TendexAI.Application.Features.Tenants.Queries.GetTenantsList;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints;

/// <summary>
/// Minimal API endpoints for tenant (government entity) lifecycle management.
/// All endpoints are prefixed with /api/v1/tenants and require Super Admin authorization.
/// </summary>
public static class TenantEndpoints
{
    /// <summary>
    /// Maps all tenant management endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants")
            .WithTags("Tenants")
            .WithDescription("Government entity (tenant) lifecycle management APIs")
            .RequireAuthorization();

        // GET /api/v1/tenants - List tenants with pagination and filtering
        group.MapGet("/", GetTenantsList)
            .WithName("GetTenantsList")
            .WithSummary("Retrieves a paginated list of tenants with optional search and status filtering.")
            .Produces<PagedResultDto<TenantListItemDto>>(StatusCodes.Status200OK)
            .RequireAuthorization(PermissionPolicies.TenantsView);

        // GET /api/v1/tenants/{id} - Get tenant details
        group.MapGet("/{id:guid}", GetTenantById)
            .WithName("GetTenantById")
            .WithSummary("Retrieves detailed information about a specific tenant.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.TenantsView);

        // GET /api/v1/tenants/{id}/branding - Get tenant branding configuration
        group.MapGet("/{id:guid}/branding", GetTenantBranding)
            .WithName("GetTenantBranding")
            .WithSummary("Retrieves the branding configuration (logo, colors) for a specific tenant.")
            .Produces<TenantBrandingDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.TenantsView);

        // POST /api/v1/tenants - Create new tenant
        group.MapPost("/", CreateTenant)
            .WithName("CreateTenant")
            .WithSummary("Creates a new government entity (tenant) on the platform.")
            .Produces<TenantDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
        .RequireAuthorization(PermissionPolicies.TenantsCreate);

        // PUT /api/v1/tenants/{id} - Update tenant info
        group.MapPut("/{id:guid}", UpdateTenant)
            .WithName("UpdateTenant")
            .WithSummary("Updates the basic information of an existing tenant.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization(PermissionPolicies.TenantsEdit);

        // PUT /api/v1/tenants/{id}/branding - Update tenant branding
        group.MapPut("/{id:guid}/branding", UpdateTenantBranding)
            .WithName("UpdateTenantBranding")
            .WithSummary("Updates the visual branding (logo, colors) for a tenant.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization(PermissionPolicies.TenantsEdit);

        // POST /api/v1/tenants/{id}/status - Change tenant status
        group.MapPost("/{id:guid}/status", ChangeTenantStatus)
            .WithName("ChangeTenantStatus")
            .WithSummary("Changes the lifecycle status of a tenant following valid state transitions.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.TenantsEdit);

        // POST /api/v1/tenants/{id}/provision - Trigger database provisioning
        group.MapPost("/{id:guid}/provision", ProvisionTenantDatabase)
            .WithName("ProvisionTenantDatabase")
            .WithSummary("Triggers automated database provisioning for a tenant in PendingProvisioning status.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.TenantsCreate);

        // GET /api/v1/tenants/statuses - Get available tenant statuses
        group.MapGet("/statuses", GetTenantStatuses)
            .WithName("GetTenantStatuses")
            .WithSummary("Returns all available tenant lifecycle statuses.")
            .Produces<IEnumerable<TenantStatusDto>>(StatusCodes.Status200OK)
            .RequireAuthorization(PermissionPolicies.TenantsEdit);

        // POST /api/v1/tenants/{id}/setup-admin - Setup tenant admin credentials
        group.MapPost("/{id:guid}/setup-admin", SetupTenantAdminAsync)
            .WithName("SetupTenantAdmin")
            .WithSummary("Configures the primary admin user credentials for a newly created tenant.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.TenantsSetupAdmin);

        // POST /api/v1/tenants/{id}/reset-admin-password - Operator resets tenant admin password
        group.MapPost("/{id:guid}/reset-admin-password", OperatorResetTenantAdminPasswordAsync)
            .WithName("OperatorResetTenantAdminPassword")
            .WithSummary("Operator-initiated password reset for a tenant's primary admin.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.TenantsResetAdminPassword);

        // GET /api/v1/tenants/resolve?hostname={hostname} - Resolve tenant by hostname (public, no auth)
        // TASK-905: Tenant resolve endpoint is registered outside the group
        // to ensure it works with CORS and does not require authentication.
        app.MapGet("/api/v1/tenants/resolve", ResolveTenantByHostname)
            .WithTags("Tenants")
            .WithName("ResolveTenantByHostname")
            .WithSummary("Resolves a tenant by hostname or subdomain. Used by the frontend to auto-detect tenant on login.")
            .AllowAnonymous()
            .RequireCors("TendexCorsPolicy")
            .Produces<TenantResolveDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // ----- Endpoint Handlers -----

    /// <summary>
    /// Retrieves a paginated list of tenants.
    /// </summary>
    private static async Task<IResult> GetTenantsList(
        ISender mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] TenantStatus? status = null)
    {
        var query = new GetTenantsListQuery(
            PageNumber: page,
            PageSize: pageSize,
            SearchTerm: search,
            StatusFilter: status);

        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.BadRequest(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Retrieves a single tenant by ID.
    /// </summary>
    private static async Task<IResult> GetTenantById(
        ISender mediator,
        Guid id)
    {
        var query = new GetTenantByIdQuery(id);
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.NotFound(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Retrieves the branding configuration for a specific tenant.
    /// </summary>
    private static async Task<IResult> GetTenantBranding(
        ISender mediator,
        Guid id)
    {
        var query = new GetTenantBrandingQuery(id);
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.NotFound(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    private static async Task<IResult> CreateTenant(
        ISender mediator,
        [FromBody] CreateTenantRequest request)
    {
        var command = new CreateTenantCommand(
            NameAr: request.NameAr,
            NameEn: request.NameEn,
            Identifier: request.Identifier,
            Subdomain: request.Subdomain,
            ContactPersonName: request.ContactPersonName,
            ContactPersonEmail: request.ContactPersonEmail,
            ContactPersonPhone: request.ContactPersonPhone,
            Notes: request.Notes,
            LogoUrl: request.LogoUrl,
            PrimaryColor: request.PrimaryColor,
            SecondaryColor: request.SecondaryColor);

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("already exists") == true)
                return Results.Conflict(new { result.Error });

            return Results.BadRequest(new { result.Error });
        }

        return Results.Created($"/api/v1/tenants/{result.Value!.Id}", result.Value);
    }

    /// <summary>
    /// Updates an existing tenant's information.
    /// </summary>
    private static async Task<IResult> UpdateTenant(
        ISender mediator,
        Guid id,
        [FromBody] UpdateTenantRequest request)
    {
        var command = new UpdateTenantCommand(
            TenantId: id,
            NameAr: request.NameAr,
            NameEn: request.NameEn,
            ContactPersonName: request.ContactPersonName,
            ContactPersonEmail: request.ContactPersonEmail,
            ContactPersonPhone: request.ContactPersonPhone,
            Notes: request.Notes);

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
            return Results.NotFound(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Updates a tenant's visual branding.
    /// </summary>
    private static async Task<IResult> UpdateTenantBranding(
        ISender mediator,
        Guid id,
        [FromBody] UpdateTenantBrandingRequest request)
    {
        var command = new UpdateTenantBrandingCommand(
            TenantId: id,
            LogoUrl: request.LogoUrl,
            PrimaryColor: request.PrimaryColor,
            SecondaryColor: request.SecondaryColor);

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
            return Results.NotFound(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Changes a tenant's lifecycle status.
    /// </summary>
    private static async Task<IResult> ChangeTenantStatus(
        ISender mediator,
        Guid id,
        [FromBody] ChangeTenantStatusRequest request)
    {
        var command = new ChangeTenantStatusCommand(
            TenantId: id,
            NewStatus: request.NewStatus);

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
            return Results.BadRequest(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Triggers database provisioning for a tenant.
    /// </summary>
    private static async Task<IResult> ProvisionTenantDatabase(
        ISender mediator,
        Guid id)
    {
        var command = new ProvisionTenantDatabaseCommand(id);
        var result = await mediator.Send(command);

        if (!result.IsSuccess)
            return Results.BadRequest(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Returns all available tenant lifecycle statuses.
    /// </summary>
    private static IResult GetTenantStatuses()
    {
        var statuses = Enum.GetValues<TenantStatus>()
            .Select(s => new TenantStatusDto(
                Value: (int)s,
                Name: s.ToString()))
            .ToList();

        return Results.Ok(statuses);
    }

    /// <summary>
    /// Resolves a tenant by hostname. Supports:
    /// 1. Subdomain matching (e.g., "mof.netaq.pro" → subdomain "mof")
    /// 2. Base domain matching (e.g., "netaq.pro" → returns Platform Operator tenant)
    /// This endpoint is public (AllowAnonymous) so the frontend can auto-detect
    /// the tenant before the user logs in.
    /// </summary>
    private static async Task<IResult> ResolveTenantByHostname(
        ITenantRepository tenantRepository,
        IMasterPlatformDbContext dbContext,
        IFileStorageService fileStorageService,
        [FromQuery] string? hostname = null)
    {
        if (string.IsNullOrWhiteSpace(hostname))
            return Results.BadRequest(new { Error = "hostname query parameter is required." });

        // Normalize hostname
        hostname = hostname.Trim().ToLowerInvariant();

        // Remove port if present (e.g., "localhost:5173")
        var colonIndex = hostname.IndexOf(':');
        if (colonIndex > 0)
            hostname = hostname[..colonIndex];

        // Try to extract subdomain (e.g., "mof.netaq.pro" → "mof")
        var parts = hostname.Split('.');
        Tenant? tenant = null;

        if (parts.Length >= 3)
        {
            // Has subdomain: e.g., mof.netaq.pro
            var subdomain = parts[0];
            if (subdomain != "www" && subdomain != "api")
            {
                tenant = await tenantRepository.GetBySubdomainAsync(subdomain);
            }
        }

        // If no subdomain match (base domain like netaq.pro or localhost),
        // return the Platform Operator tenant by matching the base domain name
        // against the subdomain field (e.g., "netaq" for netaq.pro)
        if (tenant is null)
        {
            // Extract base domain name (e.g., "netaq" from "netaq.pro")
            var baseDomainName = parts.Length >= 2 ? parts[0] : hostname;
            tenant = await tenantRepository.GetBySubdomainAsync(baseDomainName);
        }

        // Final fallback: return the first active tenant
        if (tenant is null)
        {
            var activeTenants = await tenantRepository.GetByStatusAsync(TenantStatus.Active);
            tenant = activeTenants.Count > 0 ? activeTenants[0] : null;
        }

        if (tenant is null)
            return Results.NotFound(new { Error = "No active tenant found for the given hostname." });
        var normalizedLogoUrl = await ResolveLogoUrlAsync(
            tenant.Id,
            tenant.LogoUrl,
            dbContext,
            fileStorageService,
            CancellationToken.None);

        return Results.Ok(new TenantResolveDto(
            Id: tenant.Id,
            NameAr: tenant.NameAr,
            NameEn: tenant.NameEn,
            Subdomain: tenant.Subdomain,
            LogoUrl: normalizedLogoUrl,
            PrimaryColor: tenant.PrimaryColor,
            SecondaryColor: tenant.SecondaryColor));
    }

    private static async Task<string?> ResolveLogoUrlAsync(
        Guid tenantId,
        string? logoUrl,
        IMasterPlatformDbContext dbContext,
        IFileStorageService fileStorageService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            return logoUrl;
        }

        FileAttachment? fileAttachment = null;

        if (Guid.TryParse(logoUrl, out var fileId))
        {
            fileAttachment = await dbContext.FileAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);
        }
        else if (TryExtractStorageLocation(logoUrl, out var bucketName, out var objectKey))
        {
            fileAttachment = await dbContext.FileAttachments
                .AsNoTracking()
                .Where(f => !f.IsDeleted && f.TenantId == tenantId)
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync(
                    f => f.BucketName == bucketName && f.ObjectKey == objectKey,
                    cancellationToken);

            if (fileAttachment is null)
            {
                var fileName = GetFileName(objectKey);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    fileAttachment = await dbContext.FileAttachments
                        .AsNoTracking()
                        .Where(f => !f.IsDeleted && f.TenantId == tenantId)
                        .OrderByDescending(f => f.CreatedAt)
                        .FirstOrDefaultAsync(f => f.FileName == fileName, cancellationToken);
                }
            }

            if (fileAttachment is null)
            {
                var legacyUrlResult = await fileStorageService.GetPresignedDownloadUrlAsync(
                    objectKey,
                    bucketName,
                    null,
                    cancellationToken);

                return legacyUrlResult.IsSuccess ? legacyUrlResult.Value : logoUrl;
            }
        }

        if (fileAttachment is null)
        {
            return logoUrl;
        }

        var urlResult = await fileStorageService.GetPresignedDownloadUrlAsync(
            fileAttachment.ObjectKey,
            fileAttachment.BucketName,
            null,
            cancellationToken);

        return urlResult.IsSuccess ? urlResult.Value : logoUrl;
    }

    private static string? GetFileName(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return null;
        }

        var segments = objectKey
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Length == 0 ? null : segments[^1];
    }

    private static bool TryExtractStorageLocation(string logoUrl, out string bucketName, out string objectKey)
    {
        bucketName = string.Empty;
        objectKey = string.Empty;

        var path = ExtractPath(logoUrl);
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length < 2)
        {
            return false;
        }

        var bucketIndex = string.Equals(segments[0], "minio", StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;

        if (segments.Length <= bucketIndex + 1)
        {
            return false;
        }

        bucketName = segments[bucketIndex];
        objectKey = WebUtility.UrlDecode(string.Join('/', segments.Skip(bucketIndex + 1)));
        return !string.IsNullOrWhiteSpace(bucketName) && !string.IsNullOrWhiteSpace(objectKey);
    }

    private static string? ExtractPath(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.AbsolutePath;
        }

        if (Uri.TryCreate(value, UriKind.Relative, out var relativeUri))
        {
            return relativeUri.OriginalString;
        }

        return null;
    }

    /// <summary>
    /// Operator-initiated password reset for a tenant's primary admin.
    /// This endpoint allows the platform operator (Super Admin) to reset the password
    /// of the primary admin user in a tenant's isolated database.
    /// </summary>
    private static async Task<IResult> OperatorResetTenantAdminPasswordAsync(
        Guid id,
        [FromBody] OperatorResetTenantAdminPasswordRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var operatorUserId = GetOperatorUserId(httpContext);
        if (operatorUserId == Guid.Empty)
            return Results.Problem("Operator user ID is required.", statusCode: 401);

        var command = new OperatorResetTenantAdminPasswordCommand(
            TenantId: id,
            NewPassword: request.NewPassword,
            ConfirmPassword: request.ConfirmPassword,
            NotifyAdmin: request.NotifyAdmin,
            ForceChangeOnLogin: request.ForceChangeOnLogin,
            OperatorUserId: operatorUserId,
            OperatorName: GetOperatorName(httpContext),
            IpAddress: GetClientIpAddress(httpContext),
            UserAgent: httpContext.Request.Headers.UserAgent.FirstOrDefault());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error, statusCode: 400);
    }

    /// <summary>
    /// Configures the primary admin user credentials for a newly created tenant.
    /// Sets the admin email, name, and initial password.
    /// </summary>
    private static async Task<IResult> SetupTenantAdminAsync(
        Guid id,
        [FromBody] SetupTenantAdminRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var operatorUserId = GetOperatorUserId(httpContext);
        if (operatorUserId == Guid.Empty)
            return Results.Problem("Operator user ID is required.", statusCode: 401);

        var command = new SetupTenantAdminCommand(
            TenantId: id,
            AdminEmail: request.AdminEmail,
            FirstName: request.FirstName,
            LastName: request.LastName,
            Password: request.Password,
            ConfirmPassword: request.ConfirmPassword,
            ForceChangeOnLogin: request.ForceChangeOnLogin,
            OperatorUserId: operatorUserId,
            OperatorName: GetOperatorName(httpContext),
            IpAddress: GetClientIpAddress(httpContext),
            UserAgent: httpContext.Request.Headers.UserAgent.FirstOrDefault());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error, statusCode: 400);
    }

    // ----- Helper Methods -----

    private static Guid GetOperatorUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private static string GetOperatorName(HttpContext httpContext)
    {
        var firstName = httpContext.User.FindFirstValue("first_name");
        var lastName = httpContext.User.FindFirstValue("last_name");

        if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
            return $"{firstName} {lastName}".Trim();

        return httpContext.User.FindFirstValue(ClaimTypes.Name)
            ?? httpContext.User.FindFirstValue("name")
            ?? "مشغل المنصة";
    }

    private static string GetClientIpAddress(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

// ----- Response DTOs -----

/// <summary>
/// DTO for tenant status enumeration values.
/// </summary>
public sealed record TenantStatusDto(int Value, string Name);

/// <summary>
/// Lightweight DTO returned by the tenant resolution endpoint.
/// Contains only the information needed by the frontend to bootstrap the login page.
/// </summary>
public sealed record TenantResolveDto(
    Guid Id,
    string NameAr,
    string NameEn,
    string Subdomain,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor);
