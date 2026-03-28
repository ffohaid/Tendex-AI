using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Files.Commands.DeleteFile;
using TendexAI.Application.Files.Commands.UploadFile;
using TendexAI.Application.Files.Queries.GetFileInfo;
using TendexAI.Application.Files.Queries.GetPresignedDownloadUrl;
using TendexAI.Application.Files.Queries.GetPresignedUploadUrl;
using TendexAI.Domain.Enums;

namespace TendexAI.API.Endpoints;

/// <summary>
/// Minimal API endpoints for file management operations.
/// Provides secure upload, download, presigned URL generation, and file metadata retrieval.
/// </summary>
public static class FileEndpoints
{
    /// <summary>
    /// Maps all file-related API endpoints under /api/v1/files.
    /// </summary>
    public static void MapFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/files")
            .WithTags("Files")
            .DisableAntiforgery();

        group.MapPost("/upload", UploadFileAsync)
            .WithName("UploadFile")
            .WithSummary("Upload a file to object storage")
            .WithDescription("Uploads a file through the server to MinIO. Validates file type, size, and extension before upload.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<UploadFileResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        group.MapGet("/{fileId:guid}", GetFileInfoAsync)
            .WithName("GetFileInfo")
            .WithSummary("Get file metadata by ID")
            .WithDescription("Retrieves file attachment metadata including name, size, type, and storage location.")
            .Produces<FileInfoResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("/{fileId:guid}/download-url", GetPresignedDownloadUrlAsync)
            .WithName("GetPresignedDownloadUrl")
            .WithSummary("Generate a presigned download URL")
            .WithDescription("Generates a time-limited presigned URL for secure file download directly from MinIO.")
            .Produces<PresignedDownloadUrlResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/presigned-upload-url", GetPresignedUploadUrlAsync)
            .WithName("GetPresignedUploadUrl")
            .WithSummary("Generate a presigned upload URL")
            .WithDescription("Generates a time-limited presigned URL for direct client-to-MinIO file upload. Pre-validates file parameters.")
            .Produces<PresignedUploadUrlResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{fileId:guid}", DeleteFileAsync)
            .WithName("DeleteFile")
            .WithSummary("Delete a file")
            .WithDescription("Soft-deletes a file from the database and removes it from object storage.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("/validation-rules", GetValidationRulesAsync)
            .WithName("GetFileValidationRules")
            .WithSummary("Get file validation rules")
            .WithDescription("Returns the current file upload validation rules including allowed types, extensions, and maximum size.");
    }

    /// <summary>
    /// Handles multipart file upload through the server.
    /// </summary>
    private static async Task<IResult> UploadFileAsync(
        IFormFile file,
        [FromQuery] Guid? tenantId,
        [FromQuery] string? folderPath,
        [FromQuery] FileCategory category,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.Problem(
                title: "Bad Request",
                detail: "No file was provided or the file is empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        using var stream = file.OpenReadStream();

        var command = new UploadFileCommand(
            FileStream: stream,
            FileName: file.FileName,
            ContentType: file.ContentType,
            FileSize: file.Length,
            TenantId: tenantId,
            FolderPath: folderPath,
            Category: category);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            // Determine appropriate status code based on error
            var statusCode = result.Error!.Contains("size", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status413PayloadTooLarge
                : StatusCodes.Status400BadRequest;

            return Results.Problem(
                title: "File Upload Failed",
                detail: result.Error,
                statusCode: statusCode);
        }

        return Results.Created($"/api/v1/files/{result.Value!.FileId}", result.Value);
    }

    /// <summary>
    /// Retrieves file metadata by ID.
    /// </summary>
    private static async Task<IResult> GetFileInfoAsync(
        Guid fileId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFileInfoQuery(fileId), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Not Found",
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Generates a presigned download URL for a file.
    /// </summary>
    private static async Task<IResult> GetPresignedDownloadUrlAsync(
        Guid fileId,
        [FromQuery] int? expiryMinutes,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetPresignedDownloadUrlQuery(fileId, expiryMinutes),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Not Found",
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Generates a presigned upload URL for direct client upload.
    /// </summary>
    private static async Task<IResult> GetPresignedUploadUrlAsync(
        [FromBody] PresignedUploadUrlRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetPresignedUploadUrlQuery(
                FileName: request.FileName,
                ContentType: request.ContentType,
                FileSize: request.FileSize,
                TenantId: request.TenantId,
                FolderPath: request.FolderPath,
                ExpiryMinutes: request.ExpiryMinutes),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Validation Failed",
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Soft-deletes a file.
    /// </summary>
    private static async Task<IResult> DeleteFileAsync(
        Guid fileId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeleteFileCommand(fileId),
            cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(
                title: "Not Found",
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Returns the current file validation rules.
    /// </summary>
    private static IResult GetValidationRulesAsync(IFileValidationService validationService)
    {
        return Results.Ok(new
        {
            MaxFileSizeBytes = validationService.MaxFileSizeBytes,
            MaxFileSizeMb = validationService.MaxFileSizeBytes / (1024.0 * 1024.0),
            AllowedContentTypes = validationService.AllowedContentTypes,
            AllowedExtensions = validationService.AllowedExtensions
        });
    }
}

/// <summary>
/// Request body for presigned upload URL generation.
/// </summary>
public sealed record PresignedUploadUrlRequest(
    string FileName,
    string ContentType,
    long FileSize,
    Guid? TenantId = null,
    string? FolderPath = null,
    int? ExpiryMinutes = null);
