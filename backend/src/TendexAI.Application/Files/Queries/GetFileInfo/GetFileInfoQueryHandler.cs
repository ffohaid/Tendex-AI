using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Files.Queries.GetFileInfo;

/// <summary>
/// Handles the <see cref="GetFileInfoQuery"/> by retrieving file metadata from the database.
/// </summary>
public sealed class GetFileInfoQueryHandler : IQueryHandler<GetFileInfoQuery, FileInfoResponse>
{
    private readonly IMasterPlatformDbContext _dbContext;

    public GetFileInfoQueryHandler(IMasterPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<FileInfoResponse>> Handle(
        GetFileInfoQuery request,
        CancellationToken cancellationToken)
    {
        var file = await _dbContext.FileAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FileId && !f.IsDeleted, cancellationToken);

        if (file is null)
        {
            return Result.Failure<FileInfoResponse>(
                $"File with ID '{request.FileId}' not found.");
        }

        return Result.Success(new FileInfoResponse(
            FileId: file.Id,
            FileName: file.FileName,
            ObjectKey: file.ObjectKey,
            BucketName: file.BucketName,
            ContentType: file.ContentType,
            FileSize: file.FileSize,
            TenantId: file.TenantId,
            FolderPath: file.FolderPath,
            ETag: file.ETag,
            Category: file.Category,
            CreatedAt: file.CreatedAt,
            CreatedBy: file.CreatedBy));
    }
}
