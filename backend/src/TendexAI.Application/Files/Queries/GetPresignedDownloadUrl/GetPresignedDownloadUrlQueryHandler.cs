using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Files.Queries.GetPresignedDownloadUrl;

/// <summary>
/// Handles the <see cref="GetPresignedDownloadUrlQuery"/> by looking up the file
/// in the database and generating a presigned download URL from MinIO.
/// </summary>
public sealed class GetPresignedDownloadUrlQueryHandler
    : IQueryHandler<GetPresignedDownloadUrlQuery, PresignedDownloadUrlResponse>
{
    private readonly IMasterPlatformDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;

    public GetPresignedDownloadUrlQueryHandler(
        IMasterPlatformDbContext dbContext,
        IFileStorageService fileStorageService)
    {
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
    }

    public async Task<Result<PresignedDownloadUrlResponse>> Handle(
        GetPresignedDownloadUrlQuery request,
        CancellationToken cancellationToken)
    {
        var fileAttachment = await _dbContext.FileAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FileId && !f.IsDeleted, cancellationToken);

        if (fileAttachment is null)
        {
            return Result.Failure<PresignedDownloadUrlResponse>(
                $"File with ID '{request.FileId}' not found.");
        }

        var urlResult = await _fileStorageService.GetPresignedDownloadUrlAsync(
            fileAttachment.ObjectKey,
            fileAttachment.BucketName,
            request.ExpiryMinutes,
            cancellationToken);

        if (urlResult.IsFailure)
        {
            return Result.Failure<PresignedDownloadUrlResponse>(urlResult.Error!);
        }

        return Result.Success(new PresignedDownloadUrlResponse(
            DownloadUrl: urlResult.Value!,
            FileName: fileAttachment.FileName,
            ContentType: fileAttachment.ContentType,
            FileSize: fileAttachment.FileSize,
            ExpiryMinutes: request.ExpiryMinutes ?? 60));
    }
}
