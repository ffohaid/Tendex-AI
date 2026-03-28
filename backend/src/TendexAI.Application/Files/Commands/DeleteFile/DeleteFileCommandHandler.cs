using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Files.Commands.DeleteFile;

/// <summary>
/// Handles the <see cref="DeleteFileCommand"/> by soft-deleting the file record
/// and optionally removing it from object storage.
/// </summary>
public sealed class DeleteFileCommandHandler : ICommandHandler<DeleteFileCommand>
{
    private readonly IMasterPlatformDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFileCommandHandler(
        IMasterPlatformDbContext dbContext,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var fileAttachment = await _dbContext.FileAttachments
            .FirstOrDefaultAsync(f => f.Id == request.FileId && !f.IsDeleted, cancellationToken);

        if (fileAttachment is null)
        {
            return Result.Failure($"File with ID '{request.FileId}' not found.");
        }

        // Soft delete in database (audit trail preserved)
        fileAttachment.MarkAsDeleted(request.DeletedBy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Also remove from object storage
        await _fileStorageService.DeleteFileAsync(
            fileAttachment.ObjectKey,
            fileAttachment.BucketName,
            cancellationToken);

        return Result.Success();
    }
}
