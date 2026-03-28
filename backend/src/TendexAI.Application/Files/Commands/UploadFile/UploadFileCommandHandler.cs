using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Files.Commands.UploadFile;

/// <summary>
/// Handles the <see cref="UploadFileCommand"/> by uploading the file to MinIO
/// and persisting the metadata to the database.
/// </summary>
public sealed class UploadFileCommandHandler : ICommandHandler<UploadFileCommand, UploadFileResponse>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IMasterPlatformDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public UploadFileCommandHandler(
        IFileStorageService fileStorageService,
        IMasterPlatformDbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _fileStorageService = fileStorageService;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UploadFileResponse>> Handle(
        UploadFileCommand request,
        CancellationToken cancellationToken)
    {
        // Upload to MinIO
        var uploadResult = await _fileStorageService.UploadFileAsync(
            new FileUploadRequest
            {
                FileStream = request.FileStream,
                FileName = request.FileName,
                ContentType = request.ContentType,
                FileSize = request.FileSize,
                TenantId = request.TenantId,
                FolderPath = request.FolderPath
            },
            cancellationToken);

        if (uploadResult.IsFailure)
        {
            return Result.Failure<UploadFileResponse>(uploadResult.Error!);
        }

        var storageResult = uploadResult.Value!;

        // Create domain entity for metadata persistence
        var fileAttachment = FileAttachment.Create(
            fileName: storageResult.FileName,
            objectKey: storageResult.ObjectKey,
            bucketName: storageResult.BucketName,
            contentType: storageResult.ContentType,
            fileSize: storageResult.FileSize,
            tenantId: request.TenantId,
            folderPath: request.FolderPath,
            eTag: storageResult.ETag,
            category: request.Category);

        _dbContext.FileAttachments.Add(fileAttachment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new UploadFileResponse(
            FileId: fileAttachment.Id,
            ObjectKey: storageResult.ObjectKey,
            BucketName: storageResult.BucketName,
            FileName: storageResult.FileName,
            ContentType: storageResult.ContentType,
            FileSize: storageResult.FileSize,
            ETag: storageResult.ETag,
            UploadedAt: storageResult.UploadedAt));
    }
}
