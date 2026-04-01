using MediatR;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Application.Features.Inquiries.Dtos;
using TendexAI.Domain.Entities.Inquiries;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Inquiries.Commands.GenerateAiAnswer;

public sealed record GenerateAiAnswerCommand(
    Guid InquiryId,
    Guid TenantId,
    string? AdditionalContext,
    bool UseRag,
    string RequestedBy) : IRequest<AiAnswerResponseDto>;

public sealed class GenerateAiAnswerCommandHandler : IRequestHandler<GenerateAiAnswerCommand, AiAnswerResponseDto>
{
    private readonly IInquiryRepository _inquiryRepository;
    private readonly IAiGateway _aiGateway;
    private readonly ICompetitionRepository _competitionRepository;

    public GenerateAiAnswerCommandHandler(
        IInquiryRepository inquiryRepository,
        IAiGateway aiGateway,
        ICompetitionRepository competitionRepository)
    {
        _inquiryRepository = inquiryRepository;
        _aiGateway = aiGateway;
        _competitionRepository = competitionRepository;
    }

    public async Task<AiAnswerResponseDto> Handle(GenerateAiAnswerCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _inquiryRepository.GetByIdAsync(request.InquiryId, cancellationToken);
        if (inquiry is null)
            throw new InvalidOperationException("Inquiry not found.");

        // Get competition context for better AI answers
        var competition = await _competitionRepository.GetByIdAsync(inquiry.CompetitionId, cancellationToken);
        var competitionName = competition?.ProjectNameAr ?? "غير محدد";
        var competitionDesc = competition?.Description ?? "";
        var competitionRef = competition?.ReferenceNumber ?? "";

        // Build the AI prompt with full context
        var systemPrompt = BuildSystemPrompt(competitionName, competitionDesc);
        var userPrompt = BuildUserPrompt(inquiry, request.AdditionalContext);

        var aiRequest = new AiCompletionRequest
        {
            TenantId = request.TenantId,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            MaxTokens = 2000,
            Temperature = 0.3f
        };

        var aiResponse = await _aiGateway.GenerateCompletionAsync(aiRequest, cancellationToken);

        if (!aiResponse.IsSuccess)
        {
            throw new InvalidOperationException($"AI generation failed: {aiResponse.ErrorMessage}");
        }

        // Clean the response
        var answerText = CleanAiResponse(aiResponse.Content);

        // Calculate confidence score based on response quality indicators
        var confidenceScore = CalculateConfidenceScore(answerText, inquiry.Category);

        // Save as a draft response
        var response = inquiry.SubmitDraftAnswer(answerText, isAiGenerated: true, request.RequestedBy);
        response.SetAiMetadata(confidenceScore, aiResponse.ModelName, "كراسة الشروط والمواصفات، نظام المنافسات والمشتريات الحكومية");

        await _inquiryRepository.UpdateAsync(inquiry, cancellationToken);
        await _inquiryRepository.SaveChangesAsync(cancellationToken);

        return new AiAnswerResponseDto
        {
            AnswerText = answerText,
            ConfidenceScore = confidenceScore,
            ModelUsed = aiResponse.ModelName,
            Sources = response.AiSources,
            ResponseId = response.Id
        };
    }

    private static string BuildSystemPrompt(string competitionName, string competitionDesc)
    {
        return string.Join("\n", new[]
        {
            "أنت مساعد ذكي متخصص في الإجابة على استفسارات الموردين المتعلقة بالمنافسات والمشتريات الحكومية السعودية.",
            "يجب أن تكون إجاباتك:",
            "1. دقيقة ومهنية ومتوافقة مع نظام المنافسات والمشتريات الحكومية ولائحته التنفيذية",
            "2. باللغة العربية الفصحى الرسمية",
            "3. واضحة ومباشرة دون غموض",
            "4. مستندة إلى كراسة الشروط والمواصفات الخاصة بالمنافسة",
            "5. محايدة ولا تفضل أي مورد على آخر",
            "6. لا تكشف عن معلومات سرية أو تنافسية",
            "",
            $"المنافسة الحالية: {competitionName}",
            string.IsNullOrEmpty(competitionDesc) ? "" : $"وصف المنافسة: {competitionDesc}",
            "",
            "قواعد مهمة:",
            "- إذا كان الاستفسار يتعلق بتعديل في الشروط، أوضح أن ذلك يتطلب موافقة الجهة المختصة",
            "- إذا كان الاستفسار عن مواعيد، أحل إلى الجدول الزمني المعتمد في كراسة الشروط",
            "- إذا كان الاستفسار فنياً متخصصاً، قدم إجابة عامة مع التنويه بمراجعة المواصفات الفنية التفصيلية",
            "- لا تستخدم أي تنسيق Markdown في الإجابة (لا نجوم، لا عناوين، لا قوائم مرقمة بنجوم)",
            "- استخدم فقط النص العادي مع الترقيم العربي عند الحاجة"
        });
    }

    private static string BuildUserPrompt(Inquiry inquiry, string? additionalContext)
    {
        var parts = new List<string>
        {
            $"تصنيف الاستفسار: {GetCategoryArabic(inquiry.Category)}",
            $"الأولوية: {GetPriorityArabic(inquiry.Priority)}"
        };

        if (!string.IsNullOrEmpty(inquiry.SupplierName))
            parts.Add($"المورد: {inquiry.SupplierName}");

        parts.Add("");
        parts.Add("نص الاستفسار:");
        parts.Add(inquiry.QuestionText);

        if (!string.IsNullOrEmpty(additionalContext))
        {
            parts.Add("");
            parts.Add("سياق إضافي:");
            parts.Add(additionalContext);
        }

        parts.Add("");
        parts.Add("يرجى تقديم إجابة مهنية ودقيقة على هذا الاستفسار.");

        return string.Join("\n", parts);
    }

    private static string GetCategoryArabic(InquiryCategory category) => category switch
    {
        InquiryCategory.General => "عام",
        InquiryCategory.Technical => "فني",
        InquiryCategory.Financial => "مالي",
        InquiryCategory.Administrative => "إداري",
        InquiryCategory.Legal => "قانوني",
        _ => "عام"
    };

    private static string GetPriorityArabic(InquiryPriority priority) => priority switch
    {
        InquiryPriority.Low => "منخفضة",
        InquiryPriority.Medium => "متوسطة",
        InquiryPriority.High => "عالية",
        InquiryPriority.Critical => "حرجة",
        _ => "متوسطة"
    };

    private static string CleanAiResponse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // Remove markdown formatting
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*{1,3}([^*]+)\*{1,3}", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^#{1,6}\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", "");

        return text.Trim();
    }

    private static int CalculateConfidenceScore(string answer, InquiryCategory category)
    {
        var score = 70; // Base score

        // Longer, more detailed answers get higher scores
        if (answer.Length > 200) score += 10;
        if (answer.Length > 500) score += 5;

        // Answers referencing regulations get higher scores
        if (answer.Contains("نظام المنافسات") || answer.Contains("اللائحة التنفيذية"))
            score += 10;

        // Cap at 95 (never 100% for AI)
        return Math.Min(score, 95);
    }
}
