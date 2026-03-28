using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.UserManagement.Commands.AcceptInvitation;
using TendexAI.Application.Features.UserManagement.Commands.AssignRole;
using TendexAI.Application.Features.UserManagement.Commands.RemoveRole;
using TendexAI.Application.Features.UserManagement.Commands.ResendInvitation;
using TendexAI.Application.Features.UserManagement.Commands.RevokeInvitation;
using TendexAI.Application.Features.UserManagement.Commands.SendInvitation;
using TendexAI.Application.Features.UserManagement.Commands.ToggleUserStatus;
using TendexAI.Application.Features.UserManagement.Commands.UpdateUser;
using TendexAI.Application.Features.UserManagement.Dtos;
using TendexAI.Application.Features.UserManagement.Queries.GetInvitations;
using TendexAI.Application.Features.UserManagement.Queries.GetRoles;
using TendexAI.Application.Features.UserManagement.Queries.GetUserById;
using TendexAI.Application.Features.UserManagement.Queries.GetUsers;

namespace TendexAI.API.Endpoints.UserManagement;

/// <summary>
/// Defines Minimal API endpoints for user management operations.
/// All endpoints follow RESTful conventions and return standardized responses.
/// </summary>
public static class UserManagementEndpoints
{
    /// <summary>
    /// Maps all user management endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapUserManagementEndpoints(this IEndpointRouteBuilder app)
    {
        // ----- User Endpoints -----
        var usersGroup = app.MapGroup("/api/v1/users")
            .WithTags("User Management");

        usersGroup.MapGet("/", GetUsersAsync)
            .WithName("GetUsers")
            .WithSummary("Get paginated list of users for the current tenant")
            .RequireAuthorization()
            .Produces<PaginatedResult<UserDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        usersGroup.MapGet("/{userId:guid}", GetUserByIdAsync)
            .WithName("GetUserById")
            .WithSummary("Get a specific user by ID")
            .RequireAuthorization()
            .Produces<UserDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        usersGroup.MapPut("/{userId:guid}", UpdateUserAsync)
            .WithName("UpdateUser")
            .WithSummary("Update user profile information")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        usersGroup.MapPatch("/{userId:guid}/status", ToggleUserStatusAsync)
            .WithName("ToggleUserStatus")
            .WithSummary("Activate or deactivate a user account")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        usersGroup.MapPost("/{userId:guid}/roles", AssignRoleAsync)
            .WithName("AssignRole")
            .WithSummary("Assign a role to a user")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        usersGroup.MapDelete("/{userId:guid}/roles/{roleId:guid}", RemoveRoleAsync)
            .WithName("RemoveRole")
            .WithSummary("Remove a role from a user")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // ----- Role Endpoints -----
        var rolesGroup = app.MapGroup("/api/v1/roles")
            .WithTags("Role Management");

        rolesGroup.MapGet("/", GetRolesAsync)
            .WithName("GetRoles")
            .WithSummary("Get all roles for the current tenant")
            .RequireAuthorization()
            .Produces<IReadOnlyList<RoleDto>>(StatusCodes.Status200OK);

        // ----- Invitation Endpoints -----
        var invitationsGroup = app.MapGroup("/api/v1/invitations")
            .WithTags("Invitation Management");

        invitationsGroup.MapGet("/", GetInvitationsAsync)
            .WithName("GetInvitations")
            .WithSummary("Get paginated list of invitations for the current tenant")
            .RequireAuthorization()
            .Produces<PaginatedResult<InvitationDto>>(StatusCodes.Status200OK);

        invitationsGroup.MapPost("/", SendInvitationAsync)
            .WithName("SendInvitation")
            .WithSummary("Send a registration invitation to a new user")
            .RequireAuthorization()
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        invitationsGroup.MapPost("/accept", AcceptInvitationAsync)
            .WithName("AcceptInvitation")
            .WithSummary("Accept an invitation and complete registration")
            .AllowAnonymous()
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        invitationsGroup.MapPost("/{invitationId:guid}/resend", ResendInvitationAsync)
            .WithName("ResendInvitation")
            .WithSummary("Resend an invitation email")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        invitationsGroup.MapDelete("/{invitationId:guid}", RevokeInvitationAsync)
            .WithName("RevokeInvitation")
            .WithSummary("Revoke a pending invitation")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }

    // ===== User Handlers =====

    private static async Task<IResult> GetUsersAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var query = new GetUsersQuery(tenantId, page > 0 ? page : 1, pageSize > 0 ? pageSize : 20);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> GetUserByIdAsync(
        Guid userId,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var query = new GetUserByIdQuery(userId, tenantId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 404);
    }

    private static async Task<IResult> UpdateUserAsync(
        Guid userId,
        [FromBody] UpdateUserRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var command = new UpdateUserCommand(userId, request.FirstName, request.LastName, request.PhoneNumber, tenantId);
        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> ToggleUserStatusAsync(
        Guid userId,
        [FromBody] ToggleUserStatusRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var command = new ToggleUserStatusCommand(userId, request.Activate, tenantId);
        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> AssignRoleAsync(
        Guid userId,
        [FromBody] AssignRoleRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        var currentUserId = GetCurrentUserId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var command = new AssignRoleCommand(userId, request.RoleId, tenantId, currentUserId.ToString());
        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var command = new RemoveRoleCommand(userId, roleId, tenantId);
        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error, statusCode: 400);
    }

    // ===== Role Handlers =====

    private static async Task<IResult> GetRolesAsync(
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var query = new GetRolesQuery(tenantId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    // ===== Invitation Handlers =====

    private static async Task<IResult> GetInvitationsAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var query = new GetInvitationsQuery(tenantId, page > 0 ? page : 1, pageSize > 0 ? pageSize : 20);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> SendInvitationAsync(
        [FromBody] SendInvitationRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        var currentUserId = GetCurrentUserId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        var command = new SendInvitationCommand(
            Email: request.Email,
            FirstNameAr: request.FirstNameAr,
            LastNameAr: request.LastNameAr,
            FirstNameEn: request.FirstNameEn,
            LastNameEn: request.LastNameEn,
            RoleId: request.RoleId,
            TenantId: tenantId,
            InvitedByUserId: currentUserId,
            InviterName: GetCurrentUserName(httpContext),
            TenantName: request.TenantName ?? "Tendex AI",
            BaseUrl: request.BaseUrl ?? baseUrl);

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/v1/invitations/{result.Value}", new { Id = result.Value })
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> AcceptInvitationAsync(
        [FromBody] AcceptInvitationRequest request,
        ISender mediator)
    {
        var command = new AcceptInvitationCommand(
            Token: request.Token,
            Password: request.Password,
            ConfirmPassword: request.ConfirmPassword,
            PhoneNumber: request.PhoneNumber);

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/v1/users/{result.Value}", new { Id = result.Value })
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> ResendInvitationAsync(
        Guid invitationId,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        var command = new ResendInvitationCommand(
            InvitationId: invitationId,
            TenantId: tenantId,
            InviterName: GetCurrentUserName(httpContext),
            TenantName: "Tendex AI",
            BaseUrl: baseUrl);

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> RevokeInvitationAsync(
        Guid invitationId,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var command = new RevokeInvitationCommand(invitationId, tenantId);
        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error, statusCode: 400);
    }

    // ===== Helper Methods =====

    private static Guid GetTenantId(HttpContext httpContext)
    {
        var tenantClaim = httpContext.User.FindFirstValue("tenant_id");
        return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : Guid.Empty;
    }

    private static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private static string GetCurrentUserName(HttpContext httpContext)
    {
        return httpContext.User.FindFirstValue(ClaimTypes.Name)
            ?? httpContext.User.FindFirstValue("name")
            ?? "System";
    }
}
