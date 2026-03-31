using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Common;

namespace TendexAI.Infrastructure.AI;

/// <summary>
/// Infrastructure implementation of the AI-powered technical offer analysis service.
/// Uses the unified AI Gateway with RAG context to analyze supplier offers against
/// the terms and specifications booklet (كراسة الشروط والمواصفات).
/// 
/// Architecture:
/// - Constructs structured Arabic prompts following RAG Guidelines XML tag structure
/// - Uses Few-Shot examples for consistent output format
/// - Enforces blind evaluation (no supplier identity in prompts)
/// - Parses structured JSON response from AI into domain models
/// - Logs analysis metrics (latency, tokens, model used)
/// 
/// Per RAG Guidelines:
/// - Section 2.2: AI as Copilot, not decision maker
/// - Section 2.3: Blind evaluation enforcement
/// - Section 3.4: Grounding & Citation (extract-then-analyze)
/// - Section 5.5: Offer summarization
/// - Section 5.7: Assisted auto-evaluation
/// </summary>
public sealed class AiOfferAnalysisService : IAiOfferAnalysisService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private readonly IAiGateway _aiGateway;
    private readonly ILogger<AiOfferAnalysisService> _logger;

    public AiOfferAnalysisService(
        IAiGateway aiGateway,
        ILogger<AiOfferAnalysisService> logger)
    {
        _aiGateway = aiGateway;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AiOfferAnalysisResult>> AnalyzeOfferAsync(
        AiOfferAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Build the structured system prompt
            var systemPrompt = BuildSystemPrompt();

            // 2. Build the user prompt with offer content, booklet, and criteria
            var userPrompt = BuildUserPrompt(request);

            // 3. Build RAG context from booklet content
            var ragContext = BuildRagContext(request);

            _logger.LogInformation(
                "Starting AI offer analysis for blind code {BlindCode} in competition {CompetitionId}. " +
                "Criteria count: {CriteriaCount}",
                request.BlindCode, request.CompetitionId, request.Criteria.Count);

            // 4. Send to AI Gateway
            var aiRequest = new AiCompletionRequest
            {
                TenantId = request.TenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                RagContext = ragContext,
                MaxTokensOverride = 8000,
                TemperatureOverride = 0.1 // Low temperature for analytical consistency
            };

            var aiResponse = await _aiGateway.GenerateCompletionAsync(aiRequest, cancellationToken);

            stopwatch.Stop();

            if (!aiResponse.IsSuccess)
            {
                _logger.LogWarning(
                    "AI offer analysis failed for blind code {BlindCode}: {Error}",
                    request.BlindCode, aiResponse.ErrorMessage);

                return Result.Failure<AiOfferAnalysisResult>(
                    $"AI analysis failed: {aiResponse.ErrorMessage}");
            }

            // 5. Parse the structured response
            var parseResult = ParseAiResponse(
                aiResponse.Content,
                request,
                aiResponse.Provider.ToString(),
                aiResponse.ModelName,
                stopwatch.ElapsedMilliseconds);

            if (parseResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to parse AI response for blind code {BlindCode}: {Error}",
                    request.BlindCode, parseResult.Error);

                return parseResult;
            }

            _logger.LogInformation(
                "AI offer analysis completed for blind code {BlindCode}. " +
                "Compliance score: {Score}%, Latency: {LatencyMs}ms, " +
                "Tokens: {PromptTokens}+{CompletionTokens}={TotalTokens}",
                request.BlindCode,
                parseResult.Value!.OverallComplianceScore,
                stopwatch.ElapsedMilliseconds,
                aiResponse.PromptTokens,
                aiResponse.CompletionTokens,
                aiResponse.TotalTokens);

            return parseResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Unhandled exception during AI offer analysis for blind code {BlindCode}",
                request.BlindCode);

            return Result.Failure<AiOfferAnalysisResult>(
                $"An unexpected error occurred during AI analysis: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Prompt Construction
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds the system prompt following RAG Guidelines XML tag structure.
    /// Per RAG Guidelines Section 4 (Prompt Engineering).
    /// </summary>
    private static string BuildSystemPrompt()
    {
        return """
            <role>
            أنت محلل فني متخصص في تقييم العروض الفنية للمنافسات الحكومية في المملكة العربية السعودية.
            لديك خبرة عميقة في:
            - تحليل العروض الفنية ومطابقتها مع كراسات الشروط والمواصفات
            - نظام المنافسات والمشتريات الحكومية السعودي
            - تقييم الجوانب الفنية والإدارية والمالية للعروض
            - تحديد نقاط القوة والضعف والمخاطر في العروض المقدمة
            أنت مساعد ذكي (Copilot) للجنة الفحص، وليس صانع قرار. جميع مخرجاتك هي مسودات مقترحة تخضع لمراجعة واعتماد اللجنة البشرية.
            </role>

            <instructions>
            1. التزم باللغة العربية الفصحى في جميع المخرجات (باستثناء المصطلحات التقنية المتعارف عليها).
            2. لا تذكر أي معلومات عن هوية المورد أو اسم الشركة المقدمة — التقييم أعمى بالكامل.
            3. استخدم منهجية "الاستخراج ثم التحليل":
               - الخطوة الأولى: استخرج النصوص ذات الصلة حرفياً من العرض مع ذكر رقم الصفحة/القسم.
               - الخطوة الثانية: ابنِ تحليلك بناءً على تلك الاقتباسات فقط.
            4. لا تضع درجة بدون مبرر، ولا مبرر بدون اقتباس من المستند الأصلي.
            5. إذا لم تجد معلومات كافية في العرض لتقييم معيار معين، صرّح بذلك بوضوح واقترح "يحتاج مراجعة بشرية".
            6. لا تولّد أو تفترض معلومات غير موجودة في المستندات المقدمة (منع الهلوسة).
            7. كن موضوعياً ومحايداً في التقييم — لا تميل لصالح أو ضد أي عرض.
            8. قدم توصيات واضحة وقابلة للتنفيذ للجنة الفحص.
            9. صنّف مستوى الامتثال لكل معيار: متوافق بالكامل، متوافق جزئياً، غير متوافق، يحتاج مراجعة بشرية.
            10. أعد الإجابة بتنسيق JSON المحدد أدناه بدقة.
            </instructions>

            <examples>
            مثال على تحليل معيار واحد:
            {
              "criterionId": "GUID",
              "criterionNameAr": "الفريق الفني المقترح",
              "suggestedScore": 75,
              "maxScore": 100,
              "detailedJustification": "يتضمن العرض فريقاً فنياً مكوناً من 8 أعضاء بخبرات متنوعة. مدير المشروع يحمل شهادة PMP مع خبرة 12 سنة في مشاريع مماثلة. ومع ذلك، لم يتم تقديم سير ذاتية لاثنين من أعضاء الفريق المقترحين مما يمثل نقصاً في المتطلبات.",
              "offerCitations": "صفحة 15: 'يتكون الفريق الفني من 8 متخصصين بقيادة مدير مشروع حاصل على شهادة PMP بخبرة 12 عاماً'. صفحة 18: 'مرفق السير الذاتية لـ 6 من أعضاء الفريق'.",
              "bookletRequirementReference": "البند 5.2 من كراسة الشروط - متطلبات الفريق الفني: يجب تقديم سير ذاتية لجميع أعضاء الفريق المقترح",
              "complianceNotes": "تم استيفاء متطلب الفريق الفني جزئياً. تم تقديم 6 من 8 سير ذاتية مطلوبة. ينقص سيرتان ذاتيتان.",
              "complianceLevel": "PartiallyCompliant"
            }
            </examples>

            <output_format>
            أعد الإجابة بتنسيق JSON التالي بدقة (بدون أي نص إضافي خارج JSON):
            {
              "executiveSummary": "ملخص تنفيذي شامل للعرض...",
              "strengthsAnalysis": "تحليل نقاط القوة مع الاقتباسات...",
              "weaknessesAnalysis": "تحليل نقاط الضعف مع الاقتباسات...",
              "risksAnalysis": "تحليل المخاطر المحتملة...",
              "complianceAssessment": "تقييم الامتثال مع كراسة الشروط...",
              "overallRecommendation": "التوصية العامة للجنة الفحص...",
              "overallComplianceScore": 75.5,
              "criterionResults": [
                {
                  "criterionId": "GUID",
                  "criterionNameAr": "اسم المعيار",
                  "suggestedScore": 80,
                  "maxScore": 100,
                  "detailedJustification": "المبرر التفصيلي...",
                  "offerCitations": "الاقتباسات من العرض...",
                  "bookletRequirementReference": "مرجع المتطلب من الكراسة...",
                  "complianceNotes": "ملاحظات الامتثال...",
                  "complianceLevel": "FullyCompliant|PartiallyCompliant|NonCompliant|RequiresHumanReview|NotApplicable"
                }
              ]
            }
            </output_format>
            """;
    }

    /// <summary>
    /// Builds the user prompt with offer content, criteria, and analysis instructions.
    /// </summary>
    private static string BuildUserPrompt(AiOfferAnalysisRequest request)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<input>");
        sb.AppendLine($"<project_name>{request.ProjectNameAr}</project_name>");
        sb.AppendLine($"<blind_code>{request.BlindCode}</blind_code>");
        sb.AppendLine($"<minimum_passing_score>{request.MinimumPassingScore}</minimum_passing_score>");

        // Add evaluation criteria
        sb.AppendLine("<evaluation_criteria>");
        foreach (var criterion in request.Criteria)
        {
            sb.AppendLine($"  <criterion>");
            sb.AppendLine($"    <id>{criterion.Id}</id>");
            sb.AppendLine($"    <name_ar>{criterion.NameAr}</name_ar>");
            sb.AppendLine($"    <name_en>{criterion.NameEn}</name_en>");
            sb.AppendLine($"    <description_ar>{criterion.DescriptionAr ?? "غير محدد"}</description_ar>");
            sb.AppendLine($"    <weight_percentage>{criterion.WeightPercentage}</weight_percentage>");
            sb.AppendLine($"    <max_score>{criterion.MaxScore}</max_score>");
            sb.AppendLine($"    <minimum_passing_score>{criterion.MinimumPassingScore?.ToString() ?? "غير محدد"}</minimum_passing_score>");
            sb.AppendLine($"  </criterion>");
        }
        sb.AppendLine("</evaluation_criteria>");

        // Add the offer content
        sb.AppendLine("<technical_offer>");
        sb.AppendLine(SanitizeContent(request.OfferContent));
        sb.AppendLine("</technical_offer>");

        sb.AppendLine("</input>");

        sb.AppendLine();
        sb.AppendLine("المطلوب: قم بتحليل العرض الفني أعلاه ومطابقته مع كراسة الشروط والمواصفات المقدمة في السياق المرجعي. ");
        sb.AppendLine("قيّم كل معيار من معايير التقييم المحددة وقدم درجة مقترحة مع مبرر تفصيلي واقتباسات مباشرة من العرض.");
        sb.AppendLine("تذكر: هذا تقييم أعمى — لا تذكر أي معلومات تكشف هوية المورد.");
        sb.AppendLine("أعد الإجابة بتنسيق JSON المحدد في تعليمات النظام.");

        return sb.ToString();
    }

    /// <summary>
    /// Builds the RAG context from the booklet content.
    /// </summary>
    private static string BuildRagContext(AiOfferAnalysisRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("كراسة الشروط والمواصفات:");
        sb.AppendLine("---");
        sb.AppendLine(SanitizeContent(request.BookletContent));
        sb.AppendLine("---");
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Response Parsing
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Parses the AI response JSON into a structured AiOfferAnalysisResult.
    /// </summary>
    private Result<AiOfferAnalysisResult> ParseAiResponse(
        string aiContent,
        AiOfferAnalysisRequest request,
        string providerUsed,
        string modelUsed,
        long latencyMs)
    {
        try
        {
            // Extract JSON from the response (handle potential markdown code blocks)
            var jsonContent = ExtractJsonFromResponse(aiContent);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return Result.Failure<AiOfferAnalysisResult>(
                    "AI response did not contain valid JSON content.");
            }

            var parsed = JsonSerializer.Deserialize<AiAnalysisJsonResponse>(jsonContent, s_jsonOptions);

            if (parsed is null)
            {
                return Result.Failure<AiOfferAnalysisResult>(
                    "Failed to deserialize AI analysis response.");
            }

            // Map criterion results
            var criterionResults = new List<CriterionAnalysisResult>();

            if (parsed.CriterionResults is not null)
            {
                foreach (var cr in parsed.CriterionResults)
                {
                    // Try to match criterion ID from request
                    Guid criterionId = Guid.Empty;
                    if (Guid.TryParse(cr.CriterionId, out var parsedId))
                    {
                        criterionId = parsedId;
                    }
                    else
                    {
                        // Try to match by name
                        var matchedCriterion = request.Criteria
                            .FirstOrDefault(c => c.NameAr == cr.CriterionNameAr);
                        if (matchedCriterion is not null)
                            criterionId = matchedCriterion.Id;
                    }

                    // Clamp score to valid range
                    var maxScore = request.Criteria
                        .FirstOrDefault(c => c.Id == criterionId)?.MaxScore ?? 100m;
                    var suggestedScore = Math.Max(0, Math.Min(cr.SuggestedScore, maxScore));

                    criterionResults.Add(new CriterionAnalysisResult
                    {
                        CriterionId = criterionId,
                        CriterionNameAr = cr.CriterionNameAr ?? "غير محدد",
                        SuggestedScore = suggestedScore,
                        MaxScore = maxScore,
                        DetailedJustification = cr.DetailedJustification ?? "لم يتم تقديم مبرر",
                        OfferCitations = cr.OfferCitations ?? "لا توجد اقتباسات",
                        BookletRequirementReference = cr.BookletRequirementReference,
                        ComplianceNotes = cr.ComplianceNotes ?? "لا توجد ملاحظات",
                        ComplianceLevel = cr.ComplianceLevel ?? "RequiresHumanReview"
                    });
                }
            }

            // Ensure all criteria from the request are covered
            foreach (var criterion in request.Criteria)
            {
                if (!criterionResults.Any(cr => cr.CriterionId == criterion.Id))
                {
                    criterionResults.Add(new CriterionAnalysisResult
                    {
                        CriterionId = criterion.Id,
                        CriterionNameAr = criterion.NameAr,
                        SuggestedScore = 0,
                        MaxScore = criterion.MaxScore,
                        DetailedJustification = "لم يتمكن الذكاء الاصطناعي من تحليل هذا المعيار — يحتاج مراجعة بشرية",
                        OfferCitations = "لا توجد اقتباسات",
                        BookletRequirementReference = null,
                        ComplianceNotes = "يحتاج مراجعة بشرية",
                        ComplianceLevel = "RequiresHumanReview"
                    });
                }
            }

            // Clamp overall compliance score
            var overallScore = Math.Max(0, Math.Min(parsed.OverallComplianceScore, 100m));

            var result = new AiOfferAnalysisResult
            {
                ExecutiveSummary = parsed.ExecutiveSummary ?? "لم يتم تقديم ملخص تنفيذي",
                StrengthsAnalysis = parsed.StrengthsAnalysis ?? "لم يتم تحديد نقاط قوة",
                WeaknessesAnalysis = parsed.WeaknessesAnalysis ?? "لم يتم تحديد نقاط ضعف",
                RisksAnalysis = parsed.RisksAnalysis ?? "لم يتم تحديد مخاطر",
                ComplianceAssessment = parsed.ComplianceAssessment ?? "لم يتم تقديم تقييم امتثال",
                OverallRecommendation = parsed.OverallRecommendation ?? "يحتاج مراجعة بشرية",
                OverallComplianceScore = overallScore,
                CriterionResults = criterionResults.AsReadOnly(),
                AiModelUsed = modelUsed,
                AiProviderUsed = providerUsed,
                AnalysisLatencyMs = latencyMs
            };

            return Result.Success(result);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response JSON for blind code {BlindCode}",
                request.BlindCode);
            return Result.Failure<AiOfferAnalysisResult>(
                $"Failed to parse AI analysis response: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts JSON content from AI response, handling markdown code blocks.
    /// </summary>
    private static string? ExtractJsonFromResponse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        // Try to extract from markdown code block
        var jsonBlockMatch = Regex.Match(content, @"```(?:json)?\s*\n?([\s\S]*?)\n?```", RegexOptions.Singleline);
        if (jsonBlockMatch.Success)
            return jsonBlockMatch.Groups[1].Value.Trim();

        // Try to find JSON object directly
        var jsonObjectMatch = Regex.Match(content, @"\{[\s\S]*\}", RegexOptions.Singleline);
        if (jsonObjectMatch.Success)
            return jsonObjectMatch.Value.Trim();

        return content.Trim();
    }

    /// <summary>
    /// Sanitizes content to prevent prompt injection attacks.
    /// Per RAG Guidelines security requirements.
    /// </summary>
    private static string SanitizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        // Remove potential prompt injection patterns
        var sanitized = content
            .Replace("<role>", "[role]")
            .Replace("</role>", "[/role]")
            .Replace("<instructions>", "[instructions]")
            .Replace("</instructions>", "[/instructions]")
            .Replace("<system>", "[system]")
            .Replace("</system>", "[/system]");

        // Limit content length to prevent token overflow (approximately 100K chars)
        const int maxLength = 100_000;
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized[..maxLength] + "\n... [تم اقتطاع المحتوى بسبب الحجم الكبير]";
        }

        return sanitized;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Internal JSON Models for Parsing
    // ═══════════════════════════════════════════════════════════════

    private sealed class AiAnalysisJsonResponse
    {
        public string? ExecutiveSummary { get; set; }
        public string? StrengthsAnalysis { get; set; }
        public string? WeaknessesAnalysis { get; set; }
        public string? RisksAnalysis { get; set; }
        public string? ComplianceAssessment { get; set; }
        public string? OverallRecommendation { get; set; }
        public decimal OverallComplianceScore { get; set; }
        public List<AiCriterionJsonResponse>? CriterionResults { get; set; }
    }

    private sealed class AiCriterionJsonResponse
    {
        public string? CriterionId { get; set; }
        public string? CriterionNameAr { get; set; }
        public decimal SuggestedScore { get; set; }
        public decimal MaxScore { get; set; }
        public string? DetailedJustification { get; set; }
        public string? OfferCitations { get; set; }
        public string? BookletRequirementReference { get; set; }
        public string? ComplianceNotes { get; set; }
        public string? ComplianceLevel { get; set; }
    }
}
