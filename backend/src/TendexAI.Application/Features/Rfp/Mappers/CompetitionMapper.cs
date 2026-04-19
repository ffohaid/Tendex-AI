using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Mappers;

/// <summary>
/// Static mapper for converting Competition domain entities to DTOs.
/// </summary>
public static class CompetitionMapper
{
    public static CompetitionListItemDto ToListItemDto(Competition entity, int offerCount = 0)
    {
        return new CompetitionListItemDto(
            Id: entity.Id,
            ReferenceNumber: entity.ReferenceNumber,
            ProjectNameAr: entity.ProjectNameAr,
            ProjectNameEn: entity.ProjectNameEn,
            CompetitionType: entity.CompetitionType,
            Status: entity.Status,
            CreationMethod: entity.CreationMethod,
            EstimatedBudget: entity.EstimatedBudget,
            Currency: entity.Currency,
            SubmissionDeadline: entity.SubmissionDeadline,
            SectionsCount: entity.Sections.Count,
            BoqItemsCount: entity.BoqItems.Count,
            AttachmentsCount: entity.Attachments.Count,
            OfferCount: offerCount,
            CreatedAt: entity.CreatedAt,
            CreatedBy: entity.CreatedBy);
    }

    public static CompetitionDetailDto ToDetailDto(Competition entity)
    {
        return new CompetitionDetailDto(
            Id: entity.Id,
            TenantId: entity.TenantId,
            ReferenceNumber: entity.ReferenceNumber,
            ProjectNameAr: entity.ProjectNameAr,
            ProjectNameEn: entity.ProjectNameEn,
            Description: entity.Description,
            CompetitionType: entity.CompetitionType,
            Status: entity.Status,
            CreationMethod: entity.CreationMethod,
            EstimatedBudget: entity.EstimatedBudget,
            Currency: entity.Currency,
            SubmissionDeadline: entity.SubmissionDeadline,
            ProjectDurationDays: entity.ProjectDurationDays,
            StartDate: entity.StartDate,
            EndDate: entity.EndDate,
            Department: entity.Department,
            FiscalYear: entity.FiscalYear,
            TechnicalPassingScore: entity.TechnicalPassingScore,
            TechnicalWeight: entity.TechnicalWeight,
            FinancialWeight: entity.FinancialWeight,
            SourceTemplateId: entity.SourceTemplateId,
            SourceCompetitionId: entity.SourceCompetitionId,
            Version: entity.Version,
            LastAutoSavedAt: entity.LastAutoSavedAt,
            CurrentWizardStep: entity.CurrentWizardStep,
            StatusChangeReason: entity.StatusChangeReason,
            ApprovedByUserId: entity.ApprovedByUserId,
            ApprovedAt: entity.ApprovedAt,
            CreatedAt: entity.CreatedAt,
            CreatedBy: entity.CreatedBy,
            LastModifiedAt: entity.LastModifiedAt,
            LastModifiedBy: entity.LastModifiedBy,
            Sections: entity.Sections.Select(ToSectionDto).ToList(),
            BoqItems: entity.BoqItems.Select(ToBoqItemDto).ToList(),
            EvaluationCriteria: entity.EvaluationCriteria.Select(ToCriterionDto).ToList(),
            Attachments: entity.Attachments.Select(ToAttachmentDto).ToList());
    }

    public static RfpSectionDto ToSectionDto(RfpSection entity)
    {
        return new RfpSectionDto(
            Id: entity.Id,
            CompetitionId: entity.CompetitionId,
            ParentSectionId: entity.ParentSectionId,
            TitleAr: entity.TitleAr,
            TitleEn: entity.TitleEn,
            SectionType: entity.SectionType,
            ContentHtml: entity.ContentHtml,
            IsMandatory: entity.IsMandatory,
            IsFromTemplate: entity.IsFromTemplate,
            DefaultTextColor: entity.DefaultTextColor,
            IsLocked: entity.IsLocked,
            SortOrder: entity.SortOrder,
            AssignedToUserId: entity.AssignedToUserId,
            CreatedAt: entity.CreatedAt,
            CreatedBy: entity.CreatedBy,
            LastModifiedAt: entity.LastModifiedAt);
    }

    public static BoqItemDto ToBoqItemDto(BoqItem entity)
    {
        return new BoqItemDto(
            Id: entity.Id,
            CompetitionId: entity.CompetitionId,
            ItemNumber: entity.ItemNumber,
            DescriptionAr: entity.DescriptionAr,
            DescriptionEn: entity.DescriptionEn,
            Unit: entity.Unit,
            Quantity: entity.Quantity,
            EstimatedUnitPrice: entity.EstimatedUnitPrice,
            EstimatedTotalPrice: entity.EstimatedTotalPrice,
            Category: entity.Category,
            SortOrder: entity.SortOrder);
    }

    public static EvaluationCriterionDto ToCriterionDto(EvaluationCriterion entity)
    {
        return new EvaluationCriterionDto(
            Id: entity.Id,
            CompetitionId: entity.CompetitionId,
            ParentCriterionId: entity.ParentCriterionId,
            NameAr: entity.NameAr,
            NameEn: entity.NameEn,
            DescriptionAr: entity.DescriptionAr,
            DescriptionEn: entity.DescriptionEn,
            WeightPercentage: entity.WeightPercentage,
            MinimumPassingScore: entity.MinimumPassingScore,
            MaxScore: entity.MaxScore,
            SortOrder: entity.SortOrder,
            IsActive: entity.IsActive);
    }

    public static RfpAttachmentDto ToAttachmentDto(RfpAttachment entity)
    {
        return new RfpAttachmentDto(
            Id: entity.Id,
            CompetitionId: entity.CompetitionId,
            FileName: entity.FileName,
            FileObjectKey: entity.FileObjectKey,
            BucketName: entity.BucketName,
            ContentType: entity.ContentType,
            FileSizeBytes: entity.FileSizeBytes,
            IsMandatory: entity.IsMandatory,
            DescriptionAr: entity.DescriptionAr,
            DescriptionEn: entity.DescriptionEn,
            SortOrder: entity.SortOrder,
            CreatedAt: entity.CreatedAt,
            CreatedBy: entity.CreatedBy);
    }
}
