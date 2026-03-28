using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.VideoAnalysis.Dtos;

/// <summary>
/// Extension methods for mapping domain entities to DTOs.
/// </summary>
public static class VideoAnalysisMappingExtensions
{
    public static VideoIntegrityAnalysisDto ToDto(this VideoIntegrityAnalysis entity)
    {
        return new VideoIntegrityAnalysisDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            CompetitionId = entity.CompetitionId,
            SupplierOfferId = entity.SupplierOfferId,
            VideoFileReference = entity.VideoFileReference,
            VideoFileName = entity.VideoFileName,
            VideoFileSizeBytes = entity.VideoFileSizeBytes,
            VideoDuration = entity.VideoDuration,
            ExpectedUserId = entity.ExpectedUserId,
            Status = entity.Status,
            StatusDisplayName = GetStatusDisplayName(entity.Status),
            TamperResult = entity.TamperResult,
            TamperResultDisplayName = GetTamperResultDisplayName(entity.TamperResult),
            TamperConfidenceScore = entity.TamperConfidenceScore,
            IdentityResult = entity.IdentityResult,
            IdentityResultDisplayName = GetIdentityResultDisplayName(entity.IdentityResult),
            IdentityConfidenceScore = entity.IdentityConfidenceScore,
            OverallConfidenceScore = entity.OverallConfidenceScore,
            AiProviderUsed = entity.AiProviderUsed?.ToString(),
            AiModelUsed = entity.AiModelUsed,
            AnalysisSummary = entity.AnalysisSummary,
            ErrorMessage = entity.ErrorMessage,
            CreatedAt = entity.CreatedAt,
            AnalysisStartedAt = entity.AnalysisStartedAt,
            AnalysisCompletedAt = entity.AnalysisCompletedAt,
            ReviewedByUserId = entity.ReviewedByUserId,
            ReviewedAt = entity.ReviewedAt,
            ReviewNotes = entity.ReviewNotes,
            Flags = entity.Flags.Select(f => f.ToDto()).ToList()
        };
    }

    public static VideoAnalysisFlagDto ToDto(this VideoAnalysisFlag entity)
    {
        return new VideoAnalysisFlagDto
        {
            Id = entity.Id,
            FlagCode = entity.FlagCode,
            Description = entity.Description,
            Severity = entity.Severity,
            Confidence = entity.Confidence
        };
    }

    private static string GetStatusDisplayName(VideoAnalysisStatus status) => status switch
    {
        VideoAnalysisStatus.Pending => "قيد الانتظار",
        VideoAnalysisStatus.InProgress => "قيد التحليل",
        VideoAnalysisStatus.Passed => "ناجح",
        VideoAnalysisStatus.Failed => "فاشل",
        VideoAnalysisStatus.Error => "خطأ",
        VideoAnalysisStatus.ManualReviewRequired => "يتطلب مراجعة يدوية",
        _ => "غير معروف"
    };

    private static string GetTamperResultDisplayName(TamperDetectionResult result) => result switch
    {
        TamperDetectionResult.NotAnalyzed => "لم يتم التحليل",
        TamperDetectionResult.Genuine => "تسجيل أصلي",
        TamperDetectionResult.ScreenRecordingSuspected => "يُشتبه في إعادة تسجيل من شاشة",
        TamperDetectionResult.EditingDetected => "تم اكتشاف تعديل",
        TamperDetectionResult.DeepfakeSuspected => "يُشتبه في تزييف عميق",
        TamperDetectionResult.MetadataInconsistency => "تناقض في البيانات الوصفية",
        TamperDetectionResult.Inconclusive => "غير حاسم",
        _ => "غير معروف"
    };

    private static string GetIdentityResultDisplayName(IdentityVerificationResult result) => result switch
    {
        IdentityVerificationResult.NotVerified => "لم يتم التحقق",
        IdentityVerificationResult.Confirmed => "تم التأكيد",
        IdentityVerificationResult.Mismatch => "عدم تطابق",
        IdentityVerificationResult.NoFaceDetected => "لم يتم اكتشاف وجه",
        IdentityVerificationResult.MultipleFacesDetected => "تم اكتشاف وجوه متعددة",
        IdentityVerificationResult.LowQuality => "جودة منخفضة",
        IdentityVerificationResult.Inconclusive => "غير حاسم",
        _ => "غير معروف"
    };
}
