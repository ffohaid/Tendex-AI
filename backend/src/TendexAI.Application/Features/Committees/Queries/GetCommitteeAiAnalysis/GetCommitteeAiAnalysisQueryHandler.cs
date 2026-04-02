using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Committees.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Queries.GetCommitteeAiAnalysis;

/// <summary>
/// Handles AI-powered analysis for a committee.
/// Uses a hybrid approach: rule-based analysis + optional AI gateway for deeper insights.
/// </summary>
public sealed class GetCommitteeAiAnalysisQueryHandler
    : IQueryHandler<GetCommitteeAiAnalysisQuery, CommitteeAiAnalysisResponseDto>
{
    private readonly ICommitteeRepository _committeeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAiGateway _aiGateway;

    public GetCommitteeAiAnalysisQueryHandler(
        ICommitteeRepository committeeRepository,
        ICurrentUserService currentUser,
        IAiGateway aiGateway)
    {
        _committeeRepository = committeeRepository;
        _currentUser = currentUser;
        _aiGateway = aiGateway;
    }

    public async Task<Result<CommitteeAiAnalysisResponseDto>> Handle(
        GetCommitteeAiAnalysisQuery request,
        CancellationToken cancellationToken)
    {
        var committee = await _committeeRepository.GetByIdWithMembersAsync(
            request.CommitteeId, cancellationToken);

        if (committee is null)
            return Result.Failure<CommitteeAiAnalysisResponseDto>("اللجنة غير موجودة.");

        // Build rule-based analysis
        var recommendations = new List<CommitteeAiRecommendationDto>();
        var risks = new List<string>();
        var summaryParts = new List<string>();
        var healthScore = 1.0;

        var activeMembers = committee.Members.Where(m => m.IsActive).ToList();
        var hasChair = activeMembers.Any(m => m.Role == CommitteeMemberRole.Chair);
        var hasSecretary = activeMembers.Any(m => m.Role == CommitteeMemberRole.Secretary);
        var daysRemaining = (committee.EndDate - DateTime.UtcNow).TotalDays;

        // ── Analysis: Member composition ──
        if (activeMembers.Count == 0)
        {
            risks.Add("اللجنة لا تحتوي على أي أعضاء نشطين");
            healthScore -= 0.4;
            recommendations.Add(new CommitteeAiRecommendationDto(
                RecommendationType: "critical",
                Title: "إضافة أعضاء للجنة",
                Description: "اللجنة لا تحتوي على أعضاء نشطين. يجب إضافة أعضاء فوراً لضمان استمرارية العمل.",
                Impact: "حرج - اللجنة غير قادرة على العمل",
                Confidence: 1.0));
        }
        else if (activeMembers.Count < 3)
        {
            risks.Add("عدد الأعضاء أقل من الحد الأدنى الموصى به (3 أعضاء)");
            healthScore -= 0.2;
            recommendations.Add(new CommitteeAiRecommendationDto(
                RecommendationType: "warning",
                Title: "زيادة عدد الأعضاء",
                Description: $"اللجنة تحتوي على {activeMembers.Count} عضو فقط. يُوصى بإضافة أعضاء إضافيين لتوزيع عبء العمل.",
                Impact: "متوسط - قد يؤثر على جودة القرارات",
                Confidence: 0.9));
        }

        // ── Analysis: Chair ──
        if (!hasChair)
        {
            risks.Add("لم يتم تعيين رئيس للجنة");
            healthScore -= 0.25;
            recommendations.Add(new CommitteeAiRecommendationDto(
                RecommendationType: "critical",
                Title: "تعيين رئيس للجنة",
                Description: "اللجنة بحاجة إلى رئيس لقيادة الاجتماعات واتخاذ القرارات. يُوصى بتعيين رئيس في أقرب وقت.",
                Impact: "حرج - اللجنة لا تستطيع اتخاذ قرارات رسمية",
                Confidence: 1.0));
        }

        // ── Analysis: Secretary ──
        if (!hasSecretary && activeMembers.Count >= 3)
        {
            recommendations.Add(new CommitteeAiRecommendationDto(
                RecommendationType: "suggestion",
                Title: "تعيين سكرتير للجنة",
                Description: "يُوصى بتعيين سكرتير لتوثيق المحاضر والقرارات وضمان حفظ السجلات.",
                Impact: "منخفض - تحسين التوثيق والمتابعة",
                Confidence: 0.85));
        }

        // ── Analysis: Term expiration ──
        if (daysRemaining < 0 && committee.Status == CommitteeStatus.Active)
        {
            risks.Add("انتهت فترة عمل اللجنة ولم يتم تمديدها أو حلها");
            healthScore -= 0.3;
            recommendations.Add(new CommitteeAiRecommendationDto(
                RecommendationType: "critical",
                Title: "تمديد فترة اللجنة أو حلها",
                Description: "انتهت فترة عمل اللجنة. يجب اتخاذ إجراء: تمديد الفترة أو حل اللجنة رسمياً.",
                Impact: "حرج - وضع قانوني غير واضح",
                Confidence: 1.0));
        }
        else if (daysRemaining >= 0 && daysRemaining < 14)
        {
            risks.Add($"فترة اللجنة تنتهي خلال {(int)daysRemaining} يوم");
            healthScore -= 0.1;
            recommendations.Add(new CommitteeAiRecommendationDto(
                RecommendationType: "warning",
                Title: "اقتراب انتهاء فترة اللجنة",
                Description: $"ستنتهي فترة عمل اللجنة خلال {(int)daysRemaining} يوم. يُوصى بالتخطيط لتمديد الفترة أو إنهاء المهام المعلقة.",
                Impact: "متوسط - يجب التخطيط المسبق",
                Confidence: 0.95));
        }

        // ── Analysis: Status ──
        if (committee.Status == CommitteeStatus.Suspended)
        {
            risks.Add("اللجنة معلقة حالياً");
            healthScore -= 0.15;
            recommendations.Add(new CommitteeAiRecommendationDto(
                RecommendationType: "warning",
                Title: "مراجعة حالة التعليق",
                Description: "اللجنة معلقة حالياً. يُوصى بمراجعة سبب التعليق واتخاذ قرار بإعادة التفعيل أو الحل.",
                Impact: "متوسط - تأخير في المهام المرتبطة",
                Confidence: 0.9));
        }

        // ── Analysis: Committee type-specific rules ──
        if (committee.Type == CommitteeType.TechnicalEvaluation ||
            committee.Type == CommitteeType.FinancialEvaluation)
        {
            if (activeMembers.Count < 3)
            {
                recommendations.Add(new CommitteeAiRecommendationDto(
                    RecommendationType: "compliance",
                    Title: "الحد الأدنى لأعضاء لجنة الفحص",
                    Description: "وفقاً لنظام المنافسات والمشتريات الحكومية، يجب ألا يقل عدد أعضاء لجنة الفحص عن 3 أعضاء.",
                    Impact: "حرج - مخالفة تنظيمية",
                    Confidence: 1.0));
            }
        }

        // ── Build summary ──
        summaryParts.Add($"اللجنة تضم {activeMembers.Count} عضو نشط");
        if (hasChair) summaryParts.Add("تم تعيين رئيس");
        if (hasSecretary) summaryParts.Add("تم تعيين سكرتير");
        summaryParts.Add($"المتبقي على انتهاء الفترة: {(daysRemaining >= 0 ? $"{(int)daysRemaining} يوم" : "منتهية")}");

        healthScore = Math.Max(healthScore, 0.0);
        var healthLabel = healthScore switch
        {
            >= 0.8 => "ممتازة",
            >= 0.6 => "جيدة",
            >= 0.4 => "تحتاج تحسين",
            _ => "حرجة"
        };

        // ── Try AI gateway for deeper analysis ──
        try
        {
            var tenantId = _currentUser.TenantId ?? Guid.Empty;
            var isAiAvailable = await _aiGateway.IsAvailableAsync(tenantId, cancellationToken);

            if (isAiAvailable)
            {
                var aiPrompt = BuildAiPrompt(committee, activeMembers, risks);
                var aiRequest = new AiCompletionRequest
                {
                    TenantId = tenantId,
                    SystemPrompt = "أنت مستشار متخصص في إدارة اللجان الحكومية وفقاً لنظام المنافسات والمشتريات الحكومية السعودي. قدم تحليلاً موجزاً ومفيداً باللغة العربية فقط. لا تستخدم اللغة الإنجليزية أبداً.",
                    UserPrompt = aiPrompt,
                    MaxTokensOverride = 800,
                    TemperatureOverride = 0.3
                };

                var aiResponse = await _aiGateway.GenerateCompletionAsync(aiRequest, cancellationToken);
                if (aiResponse.IsSuccess && !string.IsNullOrWhiteSpace(aiResponse.Content))
                {
                    summaryParts.Add(aiResponse.Content.Trim());
                }
            }
        }
        catch
        {
            // AI gateway failure is non-critical; rule-based analysis is sufficient
        }

        var insight = new CommitteeAiInsightDto(
            Summary: string.Join(". ", summaryParts),
            Recommendations: recommendations.Select(r => r.Title).ToList().AsReadOnly(),
            Risks: risks.AsReadOnly(),
            HealthScore: Math.Round(healthScore, 2),
            HealthLabel: healthLabel);

        return Result.Success(new CommitteeAiAnalysisResponseDto(
            Insight: insight,
            Recommendations: recommendations.AsReadOnly()));
    }

    private static string BuildAiPrompt(
        Committee committee,
        List<CommitteeMember> activeMembers,
        List<string> existingRisks)
    {
        var typeName = committee.Type switch
        {
            CommitteeType.TechnicalEvaluation => "لجنة فحص العروض الفنية",
            CommitteeType.FinancialEvaluation => "لجنة فحص العروض المالية",
            CommitteeType.BookletPreparation => "لجنة إعداد الكراسة",
            CommitteeType.InquiryReview => "لجنة مراجعة الاستفسارات",
            _ => "لجنة أخرى"
        };

        var statusName = committee.Status switch
        {
            CommitteeStatus.Active => "نشطة",
            CommitteeStatus.Suspended => "معلقة",
            CommitteeStatus.Dissolved => "منحلة",
            CommitteeStatus.Expired => "منتهية",
            _ => "غير محدد"
        };

        return $"""
            حلل الوضع الحالي لهذه اللجنة وقدم توصية واحدة مختصرة (جملتين كحد أقصى):
            - النوع: {typeName}
            - الحالة: {statusName}
            - عدد الأعضاء النشطين: {activeMembers.Count}
            - المخاطر المكتشفة: {(existingRisks.Count > 0 ? string.Join("، ", existingRisks) : "لا توجد")}
            - الفترة المتبقية: {(int)(committee.EndDate - DateTime.UtcNow).TotalDays} يوم
            - دائمة: {(committee.IsPermanent ? "نعم" : "لا")}
            """;
    }
}
