using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Profile.Commands;
using TendexAI.Application.Features.Profile.Dtos;
using TendexAI.Application.Features.Profile.Queries;

namespace TendexAI.API.Endpoints.Profile;

/// <summary>
/// Defines Minimal API endpoints for user profile management.
/// Allows authenticated users to view/update their own profile,
/// change password, and upload avatar.
/// </summary>
public static class ProfileEndpoints
{
    /// <summary>
    /// Maps all profile management endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/", GetProfileAsync)
            .WithName("GetProfile")
            .WithSummary("Get the current user's profile")
            .Produces<ProfileDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        group.MapPut("/", UpdateProfileAsync)
            .WithName("UpdateProfile")
            .WithSummary("Update the current user's profile information")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/change-password", ChangePasswordAsync)
            .WithName("ChangePassword")
            .WithSummary("Change the current user's password")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/avatar", UploadAvatarAsync)
            .WithName("UploadAvatar")
            .WithSummary("Upload or update the current user's avatar")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/avatar", DeleteAvatarAsync)
            .WithName("DeleteAvatar")
            .WithSummary("Remove the current user's avatar")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }

    /// <summary>
    /// GET /api/v1/profile
    /// Returns the authenticated user's profile information.
    /// </summary>
    private static async Task<IResult> GetProfileAsync(
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Problem(
                detail: "User not authenticated.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");

        var result = await mediator.Send(new GetProfileQuery(userId.Value));
        if (result.IsFailure)
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound,
                title: "Profile Not Found");

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// PUT /api/v1/profile
    /// Updates the authenticated user's profile.
    /// </summary>
    private static async Task<IResult> UpdateProfileAsync(
        [FromBody] UpdateProfileRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Problem(
                detail: "User not authenticated.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");

        var command = new UpdateProfileCommand(
            userId.Value,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.Email);

        var result = await mediator.Send(command);
        if (result.IsFailure)
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Update Failed");

        return Results.NoContent();
    }

    /// <summary>
    /// POST /api/v1/profile/change-password
    /// Changes the authenticated user's password.
    /// </summary>
    private static async Task<IResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Problem(
                detail: "User not authenticated.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");

        var command = new ChangePasswordCommand(
            userId.Value,
            request.CurrentPassword,
            request.NewPassword,
            request.ConfirmNewPassword);

        var result = await mediator.Send(command);
        if (result.IsFailure)
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Password Change Failed");

        return Results.NoContent();
    }

    /// <summary>
    /// POST /api/v1/profile/avatar
    /// Uploads a new avatar image for the authenticated user.
    /// Stores the file locally in /uploads/avatars/ directory.
    /// </summary>
    private static async Task<IResult> UploadAvatarAsync(
        IFormFile file,
        ISender mediator,
        HttpContext httpContext,
        IWebHostEnvironment env)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Problem(
                detail: "User not authenticated.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");

        // Validate file
        if (file.Length == 0)
            return Results.Problem(
                detail: "No file uploaded.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid File");

        if (file.Length > 5 * 1024 * 1024) // 5MB max
            return Results.Problem(
                detail: "File size exceeds the 5MB limit.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "File Too Large");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return Results.Problem(
                detail: "Only JPG, PNG, and WebP images are allowed.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid File Type");

        // Save file to local storage
        var uploadsDir = Path.Combine(env.ContentRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{userId.Value}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        // Delete existing avatar file if present
        foreach (var ext in allowedExtensions)
        {
            var existingFile = Path.Combine(uploadsDir, $"{userId.Value}{ext}");
            if (File.Exists(existingFile))
                File.Delete(existingFile);
        }

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        var avatarUrl = $"/uploads/avatars/{fileName}";

        var command = new UploadAvatarCommand(userId.Value, avatarUrl);
        var result = await mediator.Send(command);
        if (result.IsFailure)
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Avatar Upload Failed");

        return Results.Ok(new { avatarUrl = result.Value });
    }

    /// <summary>
    /// DELETE /api/v1/profile/avatar
    /// Removes the authenticated user's avatar.
    /// </summary>
    private static async Task<IResult> DeleteAvatarAsync(
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetUserId(httpContext);
        if (userId is null)
            return Results.Problem(
                detail: "User not authenticated.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");

        var command = new UploadAvatarCommand(userId.Value, string.Empty);
        var result = await mediator.Send(command);

        return Results.NoContent();
    }

    // ----- Helper Methods -----
    private static Guid? GetUserId(HttpContext context)
    {
        var subClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? context.User.FindFirst("sub")?.Value;
        return subClaim is not null && Guid.TryParse(subClaim, out var userId) ? userId : null;
    }
}
