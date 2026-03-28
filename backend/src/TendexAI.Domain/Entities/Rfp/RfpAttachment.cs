using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Represents a file attachment associated with an RFP booklet.
/// Supports mandatory attachments per template requirements (PRD section 8.3).
/// </summary>
public sealed class RfpAttachment : BaseEntity<Guid>
{
    private RfpAttachment() { } // EF Core constructor

    public static RfpAttachment Create(
        Guid competitionId,
        string fileName,
        string fileObjectKey,
        string bucketName,
        string contentType,
        long fileSizeBytes,
        bool isMandatory,
        string? descriptionAr,
        string? descriptionEn,
        int sortOrder,
        string createdBy)
    {
        return new RfpAttachment
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            FileName = fileName,
            FileObjectKey = fileObjectKey,
            BucketName = bucketName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            IsMandatory = isMandatory,
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public Guid CompetitionId { get; private set; }

    public string FileName { get; private set; } = default!;

    /// <summary>Object storage key (MinIO path).</summary>
    public string FileObjectKey { get; private set; } = default!;

    public string BucketName { get; private set; } = default!;

    public string ContentType { get; private set; } = default!;

    public long FileSizeBytes { get; private set; }

    /// <summary>Whether this attachment is mandatory per template requirements.</summary>
    public bool IsMandatory { get; private set; }

    public string? DescriptionAr { get; private set; }

    public string? DescriptionEn { get; private set; }

    /// <summary>Display order within the attachments list.</summary>
    public int SortOrder { get; private set; }

    // ----- Navigation -----
    public Competition Competition { get; private set; } = default!;

    // ----- Domain Methods -----

    public void UpdateDescription(string? descriptionAr, string? descriptionEn, string modifiedBy)
    {
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
