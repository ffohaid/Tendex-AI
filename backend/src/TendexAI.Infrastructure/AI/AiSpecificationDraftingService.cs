using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Common;

namespace TendexAI.Infrastructure.AI;

/// <summary>
/// Infrastructure implementation of the AI-powered specification drafting service.
/// Uses the unified AI Gateway with RAG context to generate booklet section drafts
/// grounded in reference documents (previous booklets, regulations, ECA templates).
/// 
/// Architecture:
/// - Constructs structured Arabic prompts following RAG Guidelines XML tag structure
/// - Uses Few-Shot examples for consistent output format
/// - Enforces grounding &amp; citation (extract-then-generate methodology)
/// - Parses structured JSON response from AI into domain models
/// - Logs generation metrics (latency, tokens, model used)
/// 
/// Per RAG Guidelines:
/// - Section 2.1: Arabic language sovereignty (formal Arabic Fusha only)
/// - Section 2.2: AI as Copilot, not decision maker
/// - Section 2.4: Anti-hallucination with mandatory citations
/// - Section 3.1: Mandatory XML prompt structure (role, instructions, examples, context, input)
/// - Section 3.2: Few-Shot Prompting (3-5 examples)
/// - Section 3.3: Long Context Management (data at top, instructions at bottom)
/// - Section 3.4: Grounding &amp; Citation (extract-then-analyze)
/// - Section 5.1: Booklet draft generation quality standards
/// </summary>
public sealed class AiSpecificationDraftingService : IAiSpecificationDraftingService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAiGateway _aiGateway;
    private readonly IContextRetrievalService _contextRetrievalService;
    private readonly ILogger<AiSpecificationDraftingService> _logger;

    public AiSpecificationDraftingService(
        IAiGateway aiGateway,
        IContextRetrievalService contextRetrievalService,
        ILogger<AiSpecificationDraftingService> logger)
    {
        _aiGateway = aiGateway;
        _contextRetrievalService = contextRetrievalService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Public Methods
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<Result<AiSpecificationDraftResult>> GenerateSectionDraftAsync(
        AiSpecificationDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting AI specification drafting for section '{SectionTitle}' in competition {CompetitionId}",
                request.SectionTitleAr, request.CompetitionId);

            // 1. Retrieve relevant context from RAG knowledge base
            var ragQuery = BuildRagQuery(request.ProjectDescriptionAr, request.SectionTitleAr, request.ProjectType);
            var ragResult = await RetrieveRagContextAsync(
                ragQuery, request.TenantId, request.CollectionName, cancellationToken);

            // 2. Build the structured system prompt
            var systemPrompt = BuildSectionDraftSystemPrompt();

            // 3. Build the user prompt with project context and RAG context
            var userPrompt = BuildSectionDraftUserPrompt(request, ragResult);

            // 4. Send to AI Gateway
            var aiRequest = new AiCompletionRequest
            {
                TenantId = request.TenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                RagContext = ragResult?.FormattedContext,
                MaxTokensOverride = 16000,
                TemperatureOverride = 0.2 // Low temperature for formal Arabic consistency
            };

            var aiResponse = await _aiGateway.GenerateCompletionAsync(aiRequest, cancellationToken);

            stopwatch.Stop();

            if (!aiResponse.IsSuccess)
            {
                _logger.LogWarning(
                    "AI specification drafting failed for section '{SectionTitle}': {Error}",
                    request.SectionTitleAr, aiResponse.ErrorMessage);

                return Result.Failure<AiSpecificationDraftResult>(
                    $"فشل توليد المسودة: {aiResponse.ErrorMessage}");
            }

            // 5. Parse the structured response
            var parseResult = ParseSectionDraftResponse(
                aiResponse.Content,
                aiResponse.Provider.ToString(),
                aiResponse.ModelName,
                stopwatch.ElapsedMilliseconds,
                ragResult);

            if (parseResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to parse AI response for section '{SectionTitle}': {Error}",
                    request.SectionTitleAr, parseResult.Error);
                return parseResult;
            }

            _logger.LogInformation(
                "AI specification drafting completed for section '{SectionTitle}'. " +
                "Grounding score: {Score}%, Latency: {LatencyMs}ms, " +
                "Citations: {CitationCount}, Tokens: {TotalTokens}",
                request.SectionTitleAr,
                parseResult.Value!.GroundingConfidenceScore,
                stopwatch.ElapsedMilliseconds,
                parseResult.Value.Citations.Count,
                aiResponse.TotalTokens);

            return parseResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Unhandled exception during AI specification drafting for section '{SectionTitle}'",
                request.SectionTitleAr);

            return Result.Failure<AiSpecificationDraftResult>(
                $"حدث خطأ غير متوقع أثناء توليد المسودة: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<AiSpecificationDraftResult>> RefineSectionDraftAsync(
        AiSpecificationRefineRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting AI section refinement for '{SectionTitle}' in competition {CompetitionId}",
                request.SectionTitleAr, request.CompetitionId);

            // 1. Retrieve relevant context from RAG knowledge base
            var ragQuery = $"{request.SectionTitleAr} {request.UserFeedbackAr}";
            var ragResult = await RetrieveRagContextAsync(
                ragQuery, request.TenantId, request.CollectionName, cancellationToken);

            // 2. Build prompts
            var systemPrompt = BuildRefinementSystemPrompt();
            var userPrompt = BuildRefinementUserPrompt(request, ragResult);

            // 3. Send to AI Gateway
            var aiRequest = new AiCompletionRequest
            {
                TenantId = request.TenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                RagContext = ragResult?.FormattedContext,
                MaxTokensOverride = 16000,
                TemperatureOverride = 0.2
            };

            var aiResponse = await _aiGateway.GenerateCompletionAsync(aiRequest, cancellationToken);

            stopwatch.Stop();

            if (!aiResponse.IsSuccess)
            {
                _logger.LogWarning(
                    "AI section refinement failed for '{SectionTitle}': {Error}",
                    request.SectionTitleAr, aiResponse.ErrorMessage);

                return Result.Failure<AiSpecificationDraftResult>(
                    $"فشل تحسين المسودة: {aiResponse.ErrorMessage}");
            }

            // 4. Parse response
            var parseResult = ParseSectionDraftResponse(
                aiResponse.Content,
                aiResponse.Provider.ToString(),
                aiResponse.ModelName,
                stopwatch.ElapsedMilliseconds,
                ragResult);

            _logger.LogInformation(
                "AI section refinement completed for '{SectionTitle}'. Latency: {LatencyMs}ms",
                request.SectionTitleAr, stopwatch.ElapsedMilliseconds);

            return parseResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Unhandled exception during AI section refinement for '{SectionTitle}'",
                request.SectionTitleAr);

            return Result.Failure<AiSpecificationDraftResult>(
                $"حدث خطأ غير متوقع أثناء تحسين المسودة: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<AiBookletStructureResult>> GenerateBookletStructureAsync(
        AiBookletStructureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting AI booklet structure generation for project '{ProjectName}' in competition {CompetitionId}",
                request.ProjectNameAr, request.CompetitionId);

            // 1. Retrieve relevant context from RAG knowledge base
            var ragQuery = $"هيكل كراسة شروط ومواصفات {request.ProjectType} {request.ProjectDescriptionAr}";
            var ragResult = await RetrieveRagContextAsync(
                ragQuery, request.TenantId, request.CollectionName, cancellationToken);

            // 2. Build prompts
            var systemPrompt = BuildBookletStructureSystemPrompt();
            var userPrompt = BuildBookletStructureUserPrompt(request, ragResult);

            // 3. Send to AI Gateway
            var aiRequest = new AiCompletionRequest
            {
                TenantId = request.TenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                RagContext = ragResult?.FormattedContext,
                MaxTokensOverride = 16000,
                TemperatureOverride = 0.2
            };

            var aiResponse = await _aiGateway.GenerateCompletionAsync(aiRequest, cancellationToken);

            stopwatch.Stop();

            if (!aiResponse.IsSuccess)
            {
                _logger.LogWarning(
                    "AI booklet structure generation failed for '{ProjectName}': {Error}",
                    request.ProjectNameAr, aiResponse.ErrorMessage);

                return Result.Failure<AiBookletStructureResult>(
                    $"فشل توليد هيكل الكراسة: {aiResponse.ErrorMessage}");
            }

            // 4. Parse response
            var parseResult = ParseBookletStructureResponse(
                aiResponse.Content,
                aiResponse.Provider.ToString(),
                aiResponse.ModelName,
                stopwatch.ElapsedMilliseconds,
                ragResult);

            _logger.LogInformation(
                "AI booklet structure generation completed for '{ProjectName}'. " +
                "Sections: {SectionCount}, Latency: {LatencyMs}ms",
                request.ProjectNameAr,
                parseResult.IsSuccess ? parseResult.Value!.Sections.Count : 0,
                stopwatch.ElapsedMilliseconds);

            return parseResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Unhandled exception during AI booklet structure generation for '{ProjectName}'",
                request.ProjectNameAr);

            return Result.Failure<AiBookletStructureResult>(
                $"حدث خطأ غير متوقع أثناء توليد هيكل الكراسة: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  RAG Context Retrieval
    // ═══════════════════════════════════════════════════════════════

    private async Task<ContextRetrievalResult?> RetrieveRagContextAsync(
        string query,
        Guid tenantId,
        string collectionName,
        CancellationToken cancellationToken)
    {
        try
        {
            var retrievalRequest = new ContextRetrievalRequest
            {
                Query = query,
                TenantId = tenantId,
                CollectionName = collectionName,
                TopK = 5,
                InitialCandidates = 20,
                ScoreThreshold = 0.5f
            };

            var result = await _contextRetrievalService.RetrieveContextAsync(
                retrievalRequest, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "RAG context retrieval failed: {Error}. Proceeding without RAG context.",
                    result.ErrorMessage);
                return null;
            }

            _logger.LogInformation(
                "RAG context retrieved successfully. Chunks: {ChunkCount}, " +
                "Total candidates: {TotalCandidates}, Time: {TimeMs}ms",
                result.Chunks.Count, result.TotalCandidates, result.RetrievalTimeMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "RAG context retrieval threw an exception. Proceeding without RAG context.");
            return null;
        }
    }

    private static string BuildRagQuery(string projectDescription, string sectionTitle, string projectType)
    {
        return $"كراسة شروط ومواصفات {projectType} - قسم {sectionTitle}: {projectDescription}";
    }

    // ═══════════════════════════════════════════════════════════════
    //  Section Draft Prompt Construction
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds the system prompt for section draft generation.
    /// Follows RAG Guidelines Section 3.1 (Mandatory XML structure).
    /// </summary>
    private static string BuildSectionDraftSystemPrompt()
    {
        return """
            <role>
            أنت خبير متخصص في إعداد كراسات الشروط والمواصفات للمشتريات الحكومية في المملكة العربية السعودية.
            لديك خبرة عميقة في:
            - إعداد كراسات الشروط والمواصفات وفقاً لنظام المنافسات والمشتريات الحكومية ولائحته التنفيذية
            - معايير وقوالب هيئة كفاءة الإنفاق والمشروعات الحكومية (EXPRO)
            - الدليل الاسترشادي لإعداد كراسات الشروط والمواصفات الخاصة بالمشاريع الرقمية
            - صياغة البنود الفنية والإدارية والمالية بلغة عربية فصحى قانونية دقيقة
            أنت مساعد ذكي (Copilot) لفريق إعداد الكراسة، وليس صانع قرار. جميع مخرجاتك هي مسودات مقترحة تخضع لمراجعة واعتماد الفريق البشري.
            </role>

            <instructions>
            1. التزم باللغة العربية الفصحى في جميع المخرجات. لا تستخدم اللغة الإنجليزية إلا للمصطلحات التقنية التي ليس لها مقابل عربي معتمد.
            2. لا تترجم ترجمة حرفية (Translationese) — اكتب بأسلوب عربي طبيعي وسليم نحوياً وإملائياً.
            3. استخدم الأرقام بالتنسيق الإنجليزي (0-9) ورمز الريال السعودي الجديد (SAR) للقيم المالية.
            4. اعتمد على المستندات المرجعية المقدمة في قسم <context> كأساس للصياغة:
               - الخطوة الأولى: استخرج النصوص والبنود ذات الصلة من المستندات المرجعية مع ذكر المصدر.
               - الخطوة الثانية: ابنِ محتوى القسم بناءً على تلك المراجع فقط.
            5. إذا لم تجد معلومات كافية في المستندات المرجعية لصياغة جزء معين، صرّح بذلك بوضوح:
               "لم يتم العثور على هذه المعلومة في المستندات المتاحة — يحتاج إدخال يدوي من فريق الإعداد."
            6. لا تولّد أرقاماً مالية أو تقديرات كمية دون سند من البيانات التاريخية.
            7. لا تفترض أو تخمن معلومات غير موجودة في المستندات المقدمة (منع الهلوسة).
            8. ضمّن اقتباسات مرجعية واضحة لكل بند رئيسي تصوغه.
            9. صِغ المحتوى بتنسيق HTML نظيف مناسب لمحرر النصوص الغني (Rich Text Editor).
            10. أعد الإجابة بتنسيق JSON المحدد أدناه بدقة.
            </instructions>

            <examples>
            <example>
            مثال على صياغة بند في قسم "الشروط العامة":
            {
              "contentHtml": "<h3>1. نطاق العمل</h3><p>يشمل نطاق العمل في هذا المشروع تقديم خدمات تطوير وتشغيل نظام إلكتروني متكامل وفقاً للمواصفات الفنية المحددة في هذه الكراسة. يلتزم المتعاقد بتنفيذ جميع الأعمال المطلوبة وفقاً لأحكام نظام المنافسات والمشتريات الحكومية الصادر بالمرسوم الملكي رقم (م/128) وتاريخ 1440/11/13هـ ولائحته التنفيذية.</p><p>يشمل النطاق على سبيل المثال لا الحصر:</p><ul><li>تحليل وتصميم النظام وفقاً للمتطلبات الفنية المعتمدة</li><li>تطوير وبرمجة مكونات النظام</li><li>اختبار النظام وضمان الجودة</li><li>التدريب ونقل المعرفة</li><li>الدعم الفني والصيانة خلال فترة الضمان</li></ul>",
              "contentPlainText": "1. نطاق العمل\nيشمل نطاق العمل في هذا المشروع تقديم خدمات تطوير وتشغيل نظام إلكتروني متكامل...",
              "citations": [
                {
                  "documentName": "نظام المنافسات والمشتريات الحكومية",
                  "sectionReference": "المادة الثالثة والعشرون",
                  "pageNumbers": "15",
                  "quotedText": "يجب أن تتضمن كراسة الشروط والمواصفات وصفاً دقيقاً للأعمال المطلوبة ونطاقها",
                  "usageContext": "تأسيس متطلب تحديد نطاق العمل بدقة في الكراسة"
                }
              ],
              "regulatoryReferences": [
                {
                  "regulationNameAr": "نظام المنافسات والمشتريات الحكومية",
                  "articleNumber": "المادة 23",
                  "requirementSummaryAr": "وجوب تضمين الكراسة وصفاً دقيقاً للأعمال المطلوبة"
                }
              ],
              "groundingConfidenceScore": 85.0,
              "warnings": []
            }
            </example>

            <example>
            مثال على صياغة بند مع تحذير بنقص المعلومات:
            {
              "contentHtml": "<h3>2. الجدول الزمني</h3><p>يلتزم المتعاقد بتنفيذ المشروع خلال المدة المحددة في العقد.</p><p><strong>[يحتاج إدخال يدوي]:</strong> يرجى تحديد المدة الزمنية للمشروع بالأشهر والمراحل التفصيلية من قبل فريق الإعداد.</p>",
              "contentPlainText": "2. الجدول الزمني\nيلتزم المتعاقد بتنفيذ المشروع خلال المدة المحددة في العقد...",
              "citations": [],
              "regulatoryReferences": [
                {
                  "regulationNameAr": "اللائحة التنفيذية لنظام المنافسات والمشتريات الحكومية",
                  "articleNumber": "المادة 36",
                  "requirementSummaryAr": "وجوب تحديد مدة تنفيذ العقد في وثائق المنافسة"
                }
              ],
              "groundingConfidenceScore": 40.0,
              "warnings": ["لم يتم العثور على تفاصيل الجدول الزمني في المستندات المتاحة — يحتاج إدخال يدوي من فريق الإعداد"]
            }
            </example>
            </examples>

            <output_format>
            أعد الإجابة بتنسيق JSON التالي بدقة (بدون أي نص إضافي خارج JSON):
            {
              "contentHtml": "محتوى القسم بتنسيق HTML...",
              "contentPlainText": "محتوى القسم كنص عادي...",
              "citations": [
                {
                  "documentName": "اسم المستند المرجعي",
                  "documentId": "معرف المستند (اختياري)",
                  "sectionReference": "رقم المادة أو القسم",
                  "pageNumbers": "أرقام الصفحات",
                  "quotedText": "النص المقتبس حرفياً",
                  "usageContext": "كيف تم استخدام هذا الاقتباس"
                }
              ],
              "regulatoryReferences": [
                {
                  "regulationNameAr": "اسم النظام أو اللائحة",
                  "articleNumber": "رقم المادة",
                  "requirementSummaryAr": "ملخص المتطلب التنظيمي"
                }
              ],
              "groundingConfidenceScore": 85.0,
              "warnings": ["أي تحذيرات أو ملاحظات"]
            }
            </output_format>
            """;
    }

    /// <summary>
    /// Builds the user prompt for section draft generation.
    /// Per RAG Guidelines Section 3.3: data at top, instructions at bottom.
    /// </summary>
    private static string BuildSectionDraftUserPrompt(
        AiSpecificationDraftRequest request,
        ContextRetrievalResult? ragResult)
    {
        var sb = new StringBuilder();

        // RAG context at the top (per Section 3.3 - Long Context Management)
        if (ragResult?.FormattedContext is not null)
        {
            sb.AppendLine("<context>");
            sb.AppendLine(ragResult.FormattedContext);
            sb.AppendLine("</context>");
            sb.AppendLine();
        }

        sb.AppendLine("<input>");
        sb.AppendLine($"<project_name>{request.ProjectNameAr}</project_name>");
        sb.AppendLine($"<project_description>{request.ProjectDescriptionAr}</project_description>");
        sb.AppendLine($"<project_type>{request.ProjectType}</project_type>");

        if (request.EstimatedBudget.HasValue)
        {
            sb.AppendLine($"<estimated_budget>{request.EstimatedBudget.Value:N2} SAR</estimated_budget>");
        }

        sb.AppendLine($"<section_type>{request.SectionType}</section_type>");
        sb.AppendLine($"<section_title>{request.SectionTitleAr}</section_title>");

        if (!string.IsNullOrWhiteSpace(request.AdditionalInstructions))
        {
            sb.AppendLine($"<additional_instructions>{request.AdditionalInstructions}</additional_instructions>");
        }

        sb.AppendLine("</input>");

        sb.AppendLine();
        sb.AppendLine("المطلوب: قم بصياغة محتوى القسم المحدد أعلاه لكراسة الشروط والمواصفات، مع الالتزام بالتعليمات والمستندات المرجعية المقدمة.");
        sb.AppendLine("تذكير: إذا لم تجد المعلومة في السياق المقدم، أجب بـ: لم يتم العثور على هذه المعلومة في المستندات المتاحة.");

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Refinement Prompt Construction
    // ═══════════════════════════════════════════════════════════════

    private static string BuildRefinementSystemPrompt()
    {
        return """
            <role>
            أنت خبير متخصص في تحسين وتنقيح كراسات الشروط والمواصفات للمشتريات الحكومية في المملكة العربية السعودية.
            مهمتك هي تحسين المسودة الحالية بناءً على ملاحظات المستخدم مع الحفاظ على الالتزام بالأنظمة واللوائح.
            أنت مساعد ذكي (Copilot) لفريق إعداد الكراسة، وليس صانع قرار.
            </role>

            <instructions>
            1. التزم باللغة العربية الفصحى في جميع المخرجات.
            2. حسّن المسودة الحالية بناءً على ملاحظات المستخدم المحددة.
            3. حافظ على البنود والاقتباسات المرجعية الصحيحة من المسودة الأصلية.
            4. أضف اقتباسات مرجعية جديدة عند إضافة محتوى جديد.
            5. إذا طلب المستخدم إضافة معلومات غير متوفرة في المستندات المرجعية، وضّح ذلك.
            6. لا تفترض أو تخمن معلومات غير موجودة في المستندات المقدمة.
            7. أعد الإجابة بنفس تنسيق JSON المستخدم في التوليد الأولي.
            </instructions>

            <output_format>
            أعد الإجابة بتنسيق JSON التالي بدقة (بدون أي نص إضافي خارج JSON):
            {
              "contentHtml": "المحتوى المحسّن بتنسيق HTML...",
              "contentPlainText": "المحتوى المحسّن كنص عادي...",
              "citations": [...],
              "regulatoryReferences": [...],
              "groundingConfidenceScore": 85.0,
              "warnings": [...]
            }
            </output_format>
            """;
    }

    private static string BuildRefinementUserPrompt(
        AiSpecificationRefineRequest request,
        ContextRetrievalResult? ragResult)
    {
        var sb = new StringBuilder();

        // RAG context at the top
        if (ragResult?.FormattedContext is not null)
        {
            sb.AppendLine("<context>");
            sb.AppendLine(ragResult.FormattedContext);
            sb.AppendLine("</context>");
            sb.AppendLine();
        }

        sb.AppendLine("<input>");
        sb.AppendLine($"<project_name>{request.ProjectNameAr}</project_name>");
        sb.AppendLine($"<section_title>{request.SectionTitleAr}</section_title>");
        sb.AppendLine();
        sb.AppendLine("<current_draft>");
        sb.AppendLine(request.CurrentContentHtml);
        sb.AppendLine("</current_draft>");
        sb.AppendLine();
        sb.AppendLine("<user_feedback>");
        sb.AppendLine(request.UserFeedbackAr);
        sb.AppendLine("</user_feedback>");
        sb.AppendLine("</input>");

        sb.AppendLine();
        sb.AppendLine("المطلوب: قم بتحسين المسودة الحالية بناءً على ملاحظات المستخدم أعلاه.");

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Booklet Structure Prompt Construction
    // ═══════════════════════════════════════════════════════════════

    private static string BuildBookletStructureSystemPrompt()
    {
        return """
            <role>
            أنت خبير متخصص في هيكلة كراسات الشروط والمواصفات للمشتريات الحكومية في المملكة العربية السعودية.
            لديك خبرة في قوالب هيئة كفاءة الإنفاق والمشروعات الحكومية (EXPRO) والدليل الاسترشادي لإعداد الكراسات.
            أنت مساعد ذكي (Copilot) لفريق إعداد الكراسة، وليس صانع قرار.
            </role>

            <instructions>
            1. التزم باللغة العربية الفصحى.
            2. اقترح هيكلاً كاملاً للكراسة يتضمن جميع الأقسام الإلزامية وفقاً لقوالب EXPRO.
            3. اعتمد على المستندات المرجعية في تحديد الأقسام الإلزامية.
            4. رتّب الأقسام ترتيباً منطقياً متسلسلاً.
            5. وضّح لكل قسم ما إذا كان إلزامياً أو اختيارياً.
            6. قدم وصفاً موجزاً لمحتوى كل قسم (جملة أو جملتان فقط لكل وصف).
            7. لا تتجاوز مستوى واحداً من الأقسام الفرعية (subSections). لا تضع أقساماً فرعية داخل أقسام فرعية.
            8. اجعل وصف كل قسم فرعي (descriptionAr) مختصراً جداً (لا يتجاوز 50 كلمة).
            9. أعد الإجابة بتنسيق JSON المحدد أدناه فقط. تأكد من أن JSON كامل وصالح.
            10. لا تضف أي نص قبل أو بعد كائن JSON.
            </instructions>

            <output_format>
            أعد الإجابة بتنسيق JSON التالي بدقة (بدون أي نص إضافي خارج JSON):
            {
              "sections": [
                {
                  "titleAr": "عنوان القسم بالعربية",
                  "titleEn": "Section Title in English",
                  "sectionType": "GeneralInformation|TechnicalSpecifications|TermsAndConditions|EvaluationCriteria|BillOfQuantities|Attachments|Custom",
                  "isMandatory": true,
                  "descriptionAr": "وصف موجز لمحتوى القسم",
                  "sortOrder": 1,
                  "subSections": [
                    {
                      "titleAr": "عنوان القسم الفرعي",
                      "titleEn": "Sub-Section Title",
                      "sectionType": "Custom",
                      "isMandatory": false,
                      "descriptionAr": "وصف مختصر",
                      "sortOrder": 1,
                      "subSections": []
                    }
                  ]
                }
              ],
              "structureSummaryAr": "ملخص الهيكل المقترح...",
              "citations": [
                {
                  "documentName": "اسم المستند",
                  "sectionReference": "المرجع",
                  "quotedText": "النص المقتبس",
                  "usageContext": "سياق الاستخدام"
                }
              ]
            }
            تأكد من إغلاق جميع الأقواس والمصفوفات بشكل صحيح. يجب أن يكون JSON صالحاً تماماً.
            </output_format>
            """;
    }

    private static string BuildBookletStructureUserPrompt(
        AiBookletStructureRequest request,
        ContextRetrievalResult? ragResult)
    {
        var sb = new StringBuilder();

        // RAG context at the top
        if (ragResult?.FormattedContext is not null)
        {
            sb.AppendLine("<context>");
            sb.AppendLine(ragResult.FormattedContext);
            sb.AppendLine("</context>");
            sb.AppendLine();
        }

        sb.AppendLine("<input>");
        sb.AppendLine($"<project_name>{request.ProjectNameAr}</project_name>");
        sb.AppendLine($"<project_description>{request.ProjectDescriptionAr}</project_description>");
        sb.AppendLine($"<project_type>{request.ProjectType}</project_type>");

        if (request.EstimatedBudget.HasValue)
        {
            sb.AppendLine($"<estimated_budget>{request.EstimatedBudget.Value:N2} SAR</estimated_budget>");
        }

        sb.AppendLine("</input>");

        sb.AppendLine();
        sb.AppendLine("المطلوب: اقترح هيكلاً كاملاً لكراسة الشروط والمواصفات للمشروع المحدد أعلاه، مع تضمين جميع الأقسام الإلزامية وفقاً لمعايير EXPRO.");

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Response Parsing
    // ═══════════════════════════════════════════════════════════════

    private static Result<AiSpecificationDraftResult> ParseSectionDraftResponse(
        string aiContent,
        string providerName,
        string modelName,
        long latencyMs,
        ContextRetrievalResult? ragResult)
    {
        try
        {
            // Extract JSON from response (handle potential markdown code blocks)
            var jsonContent = ExtractJsonFromResponse(aiContent);

            var parsed = JsonSerializer.Deserialize<SectionDraftJsonResponse>(jsonContent, s_jsonOptions);

            if (parsed is null)
            {
                return Result.Failure<AiSpecificationDraftResult>(
                    "فشل في تحليل استجابة الذكاء الاصطناعي: الاستجابة فارغة.");
            }

            var citations = (parsed.Citations ?? [])
                .Select(c => new AiCitation
                {
                    DocumentName = c.DocumentName ?? "غير محدد",
                    DocumentId = string.IsNullOrWhiteSpace(c.DocumentId)
                        ? null
                        : Guid.TryParse(c.DocumentId, out var docId) ? docId : null,
                    SectionReference = c.SectionReference ?? "غير محدد",
                    PageNumbers = c.PageNumbers,
                    QuotedText = c.QuotedText ?? "",
                    UsageContext = c.UsageContext ?? ""
                })
                .ToList();

            // Enrich citations with RAG chunk metadata if available
            if (ragResult?.Chunks is { Count: > 0 })
            {
                foreach (var chunk in ragResult.Chunks)
                {
                    var existingCitation = citations.FirstOrDefault(c =>
                        c.DocumentName == chunk.DocumentName);

                    if (existingCitation is null)
                    {
                        citations.Add(new AiCitation
                        {
                            DocumentName = chunk.DocumentName,
                            DocumentId = chunk.DocumentId,
                            SectionReference = chunk.SectionName ?? "غير محدد",
                            PageNumbers = chunk.PageNumbers,
                            QuotedText = chunk.Text.Length > 200
                                ? chunk.Text[..200] + "..."
                                : chunk.Text,
                            UsageContext = "سياق مرجعي من قاعدة المعرفة"
                        });
                    }
                }
            }

            var regulatoryRefs = (parsed.RegulatoryReferences ?? [])
                .Select(r => new AiRegulatoryReference
                {
                    RegulationNameAr = r.RegulationNameAr ?? "غير محدد",
                    ArticleNumber = r.ArticleNumber ?? "غير محدد",
                    RequirementSummaryAr = r.RequirementSummaryAr ?? ""
                })
                .ToList();

            return Result.Success(new AiSpecificationDraftResult
            {
                ContentHtml = parsed.ContentHtml ?? "",
                ContentPlainText = parsed.ContentPlainText ?? "",
                Citations = citations,
                RegulatoryReferences = regulatoryRefs,
                GroundingConfidenceScore = parsed.GroundingConfidenceScore,
                Warnings = parsed.Warnings ?? [],
                ProviderName = providerName,
                ModelName = modelName,
                LatencyMs = latencyMs
            });
        }
        catch (JsonException ex)
        {
            return Result.Failure<AiSpecificationDraftResult>(
                $"فشل في تحليل استجابة JSON من الذكاء الاصطناعي: {ex.Message}");
        }
    }

    private static Result<AiBookletStructureResult> ParseBookletStructureResponse(
        string aiContent,
        string providerName,
        string modelName,
        long latencyMs,
        ContextRetrievalResult? ragResult)
    {
        try
        {
            var jsonContent = ExtractJsonFromResponse(aiContent);

            var parsed = JsonSerializer.Deserialize<BookletStructureJsonResponse>(jsonContent, s_jsonOptions);

            if (parsed is null)
            {
                return Result.Failure<AiBookletStructureResult>(
                    "فشل في تحليل استجابة الذكاء الاصطناعي: الاستجابة فارغة.");
            }

            var sections = (parsed.Sections ?? [])
                .Select(MapProposedSection)
                .ToList();

            var citations = (parsed.Citations ?? [])
                .Select(c => new AiCitation
                {
                    DocumentName = c.DocumentName ?? "غير محدد",
                    SectionReference = c.SectionReference ?? "غير محدد",
                    PageNumbers = c.PageNumbers,
                    QuotedText = c.QuotedText ?? "",
                    UsageContext = c.UsageContext ?? ""
                })
                .ToList();

            return Result.Success(new AiBookletStructureResult
            {
                Sections = sections,
                StructureSummaryAr = parsed.StructureSummaryAr ?? "",
                Citations = citations,
                ProviderName = providerName,
                ModelName = modelName,
                LatencyMs = latencyMs
            });
        }
        catch (JsonException ex)
        {
            return Result.Failure<AiBookletStructureResult>(
                $"فشل في تحليل استجابة JSON من الذكاء الاصطناعي: {ex.Message}");
        }
    }

    private static ProposedSection MapProposedSection(ProposedSectionJson s)
    {
        return new ProposedSection
        {
            TitleAr = s.TitleAr ?? "غير محدد",
            TitleEn = s.TitleEn ?? "Not specified",
            SectionType = s.SectionType ?? "Custom",
            IsMandatory = s.IsMandatory,
            DescriptionAr = s.DescriptionAr ?? "",
            SortOrder = s.SortOrder,
            SubSections = s.SubSections?.Select(MapProposedSection).ToList()
        };
    }

    /// <summary>
    /// Extracts JSON content from AI response, handling potential markdown code blocks
    /// and attempting to repair truncated JSON responses.
    /// </summary>
    private static string ExtractJsonFromResponse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "{}";

        var trimmed = content.Trim();

        // Handle markdown code blocks: ```json ... ``` or ``` ... ```
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
            {
                trimmed = trimmed[(firstNewline + 1)..];
            }

            var lastBackticks = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (lastBackticks > 0)
            {
                trimmed = trimmed[..lastBackticks];
            }

            trimmed = trimmed.Trim();
        }

        // Find the first { and last } to extract JSON object
        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');

        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            return trimmed[firstBrace..(lastBrace + 1)];
        }

        // If no closing brace found, the JSON may be truncated.
        // Attempt to repair by closing all open brackets and braces.
        if (firstBrace >= 0)
        {
            return RepairTruncatedJson(trimmed[firstBrace..]);
        }

        return trimmed;
    }

    /// <summary>
    /// Attempts to repair truncated JSON by closing all unclosed brackets and braces.
    /// This handles cases where the AI response was cut off due to token limits.
    /// </summary>
    private static string RepairTruncatedJson(string truncatedJson)
    {
        var sb = new StringBuilder(truncatedJson);

        // Remove any trailing incomplete string value (cut off mid-string)
        var lastQuote = truncatedJson.LastIndexOf('"');
        if (lastQuote > 0)
        {
            // Check if the quote is an opening quote without a closing pair
            var quoteCount = 0;
            var inEscape = false;
            for (var i = 0; i < truncatedJson.Length; i++)
            {
                if (inEscape) { inEscape = false; continue; }
                if (truncatedJson[i] == '\\') { inEscape = true; continue; }
                if (truncatedJson[i] == '"') quoteCount++;
            }

            // If odd number of quotes, close the last string
            if (quoteCount % 2 != 0)
            {
                sb.Append('"');
            }
        }

        // Count unclosed brackets and braces
        var openBraces = 0;
        var openBrackets = 0;
        var inString = false;
        var escaped = false;

        foreach (var ch in sb.ToString())
        {
            if (escaped) { escaped = false; continue; }
            if (ch == '\\' && inString) { escaped = true; continue; }
            if (ch == '"') { inString = !inString; continue; }
            if (inString) continue;

            switch (ch)
            {
                case '{': openBraces++; break;
                case '}': openBraces--; break;
                case '[': openBrackets++; break;
                case ']': openBrackets--; break;
            }
        }

        // Remove trailing comma if present (invalid JSON)
        var result = sb.ToString().TrimEnd();
        if (result.EndsWith(','))
        {
            result = result[..^1];
        }

        sb = new StringBuilder(result);

        // Close all unclosed brackets and braces in reverse order
        for (var i = 0; i < openBrackets; i++) sb.Append(']');
        for (var i = 0; i < openBraces; i++) sb.Append('}');

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Internal JSON Response Models (for deserialization only)
    // ═══════════════════════════════════════════════════════════════

    private sealed record SectionDraftJsonResponse
    {
        public string? ContentHtml { get; init; }
        public string? ContentPlainText { get; init; }
        public List<CitationJson>? Citations { get; init; }
        public List<RegulatoryReferenceJson>? RegulatoryReferences { get; init; }
        public double GroundingConfidenceScore { get; init; }
        public List<string>? Warnings { get; init; }
    }

    private sealed record CitationJson
    {
        public string? DocumentName { get; init; }
        public string? DocumentId { get; init; }
        public string? SectionReference { get; init; }
        public string? PageNumbers { get; init; }
        public string? QuotedText { get; init; }
        public string? UsageContext { get; init; }
    }

    private sealed record RegulatoryReferenceJson
    {
        public string? RegulationNameAr { get; init; }
        public string? ArticleNumber { get; init; }
        public string? RequirementSummaryAr { get; init; }
    }

    private sealed record BookletStructureJsonResponse
    {
        public List<ProposedSectionJson>? Sections { get; init; }
        public string? StructureSummaryAr { get; init; }
        public List<CitationJson>? Citations { get; init; }
    }

    private sealed record ProposedSectionJson
    {
        public string? TitleAr { get; init; }
        public string? TitleEn { get; init; }
        public string? SectionType { get; init; }
        public bool IsMandatory { get; init; }
        public string? DescriptionAr { get; init; }
        public int SortOrder { get; init; }
        public List<ProposedSectionJson>? SubSections { get; init; }
    }
}
