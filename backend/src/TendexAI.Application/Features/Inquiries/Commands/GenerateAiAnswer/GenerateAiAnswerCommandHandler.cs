using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<GenerateAiAnswerCommandHandler> _logger;

    /// <summary>
    /// Maximum time to wait for the AI provider to respond.
    /// </summary>
    private static readonly TimeSpan AiTimeout = TimeSpan.FromSeconds(90);

    public GenerateAiAnswerCommandHandler(
        IInquiryRepository inquiryRepository,
        IAiGateway aiGateway,
        ICompetitionRepository competitionRepository,
        ILogger<GenerateAiAnswerCommandHandler> logger)
    {
        _inquiryRepository = inquiryRepository;
        _aiGateway = aiGateway;
        _competitionRepository = competitionRepository;
        _logger = logger;
    }

    public async Task<AiAnswerResponseDto> Handle(GenerateAiAnswerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting AI answer generation for inquiry {InquiryId}, tenant {TenantId}",
            request.InquiryId, request.TenantId);

        // 1. Load the inquiry
        var inquiry = await _inquiryRepository.GetByIdAsync(request.InquiryId, cancellationToken);
        if (inquiry is null)
        {
            _logger.LogWarning("Inquiry {InquiryId} not found", request.InquiryId);
            throw new InvalidOperationException("الاستفسار غير موجود.");
        }

        // 2. Check if AI is available for this tenant
        var isAiAvailable = await _aiGateway.IsAvailableAsync(request.TenantId, cancellationToken);
        if (!isAiAvailable)
        {
            _logger.LogWarning("No AI configuration found for tenant {TenantId}", request.TenantId);
            throw new InvalidOperationException("لم يتم تكوين خدمة الذكاء الاصطناعي لهذه الجهة. يرجى إعداد مزود الذكاء الاصطناعي من لوحة تحكم المشغل.");
        }

        // 3. Get competition context for better AI answers
        string competitionName = "غير محدد";
        string competitionDesc = "";
        try
        {
            var competition = await _competitionRepository.GetByIdAsync(inquiry.CompetitionId, cancellationToken);
            competitionName = competition?.ProjectNameAr ?? "غير محدد";
            competitionDesc = competition?.Description ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load competition {CompetitionId} for inquiry context", inquiry.CompetitionId);
        }

        // 4. Build the AI prompt with full context
        var systemPrompt = BuildSystemPrompt(competitionName, competitionDesc);
        var userPrompt = BuildUserPrompt(inquiry, request.AdditionalContext);

        var aiRequest = new AiCompletionRequest
        {
            TenantId = request.TenantId,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            MaxTokensOverride = 2000,
            TemperatureOverride = 0.3
        };

        // 5. Call AI with timeout protection
        AiCompletionResponse aiResponse;
        try
        {
            using var aiCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            aiCts.CancelAfter(AiTimeout);

            _logger.LogInformation("Calling AI gateway for inquiry {InquiryId}...", request.InquiryId);
            aiResponse = await _aiGateway.GenerateCompletionAsync(aiRequest, aiCts.Token);
            _logger.LogInformation(
                "AI gateway responded for inquiry {InquiryId}: Success={IsSuccess}, Model={Model}",
                request.InquiryId, aiResponse.IsSuccess, aiResponse.ModelName);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("AI generation timed out after {Timeout}s for inquiry {InquiryId}",
                AiTimeout.TotalSeconds, request.InquiryId);
            throw new InvalidOperationException("انتهت مهلة الاتصال بخدمة الذكاء الاصطناعي. يرجى المحاولة مرة أخرى.");
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw if the original cancellation token was cancelled
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI gateway call failed for inquiry {InquiryId}", request.InquiryId);
            throw new InvalidOperationException($"فشل الاتصال بخدمة الذكاء الاصطناعي: {ex.Message}");
        }

        if (!aiResponse.IsSuccess)
        {
            _logger.LogWarning("AI generation returned failure for inquiry {InquiryId}: {Error}",
                request.InquiryId, aiResponse.ErrorMessage);
            throw new InvalidOperationException($"فشل توليد الإجابة: {aiResponse.ErrorMessage}");
        }

        // 6. Clean the response
        var answerText = CleanAiResponse(aiResponse.Content);
        if (string.IsNullOrWhiteSpace(answerText))
        {
            _logger.LogWarning("AI returned empty response for inquiry {InquiryId}", request.InquiryId);
            throw new InvalidOperationException("أرجع الذكاء الاصطناعي إجابة فارغة. يرجى المحاولة مرة أخرى.");
        }

        // 7. Calculate confidence score based on response quality indicators
        var confidenceScore = CalculateConfidenceScore(answerText, inquiry.Category);

        // 8. Save as a draft response using direct INSERT to avoid EF Core change tracking issues
        InquiryResponse response;
        try
        {
            // Create the response entity directly instead of going through the Inquiry aggregate
            // to avoid DbUpdateConcurrencyException caused by EF Core tracking the Inquiry entity
            response = InquiryResponse.Create(
                inquiryId: inquiry.Id,
                answerText: answerText,
                isAiGenerated: true,
                createdBy: request.RequestedBy);
            response.SetAiMetadata(confidenceScore, aiResponse.ModelName, "كراسة الشروط والمواصفات، نظام المنافسات والمشتريات الحكومية");

            // Add the response directly to the DbContext (bypasses Inquiry change tracking)
            await _inquiryRepository.AddResponseAsync(response, cancellationToken);
            await _inquiryRepository.SaveChangesAsync(cancellationToken);

            // Update the inquiry's LastModifiedAt using ExecuteUpdate (no change tracking)
            await _inquiryRepository.UpdateInquiryFieldsAsync(
                inquiry.Id, DateTime.UtcNow, request.RequestedBy, cancellationToken);

            _logger.LogInformation(
                "AI answer saved successfully for inquiry {InquiryId}, response {ResponseId}, confidence {Score}%",
                request.InquiryId, response.Id, confidenceScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save AI answer for inquiry {InquiryId}", request.InquiryId);
            throw new InvalidOperationException($"تم توليد الإجابة بنجاح لكن فشل حفظها في قاعدة البيانات. يرجى المحاولة مرة أخرى. التفاصيل: {ex.Message}");
        }

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
