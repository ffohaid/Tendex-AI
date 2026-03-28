using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Common;

namespace TendexAI.Infrastructure.AI;

/// <summary>
/// Infrastructure implementation of the AI-powered BOQ (Bill of Quantities / جدول الكميات) generation service.
/// Uses the unified AI Gateway with RAG context to generate BOQ items grounded in
/// reference documents and historical competition data.
/// 
/// Architecture:
/// - Constructs structured Arabic prompts following RAG Guidelines XML tag structure
/// - Uses Few-Shot examples for consistent output format
/// - Enforces grounding &amp; citation — no prices without historical data support
/// - Parses structured JSON response from AI into domain-compatible models
/// - Logs generation metrics (latency, tokens, model used)
/// 
/// Per RAG Guidelines:
/// - Section 2.1: Arabic language sovereignty (formal Arabic Fusha only)
/// - Section 2.2: AI as Copilot, not decision maker
/// - Section 2.4: Anti-hallucination — no estimated prices without historical data
/// - Section 3.1: Mandatory XML prompt structure
/// - Section 3.4: Grounding &amp; Citation (extract-then-generate)
/// - Section 5.1: No financial items without historical data backing
/// </summary>
public sealed class AiBoqGenerationService : IAiBoqGenerationService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IAiGateway _aiGateway;
    private readonly IContextRetrievalService _contextRetrievalService;
    private readonly ILogger<AiBoqGenerationService> _logger;

    public AiBoqGenerationService(
        IAiGateway aiGateway,
        IContextRetrievalService contextRetrievalService,
        ILogger<AiBoqGenerationService> logger)
    {
        _aiGateway = aiGateway;
        _contextRetrievalService = contextRetrievalService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Public Methods
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<Result<AiBoqGenerationResult>> GenerateBoqAsync(
        AiBoqGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting AI BOQ generation for project '{ProjectName}' in competition {CompetitionId}",
                request.ProjectNameAr, request.CompetitionId);

            // 1. Retrieve relevant context from RAG knowledge base
            var ragQuery = BuildBoqRagQuery(request);
            var ragResult = await RetrieveRagContextAsync(
                ragQuery, request.TenantId, request.CollectionName, cancellationToken);

            // 2. Build the structured system prompt
            var systemPrompt = BuildBoqSystemPrompt();

            // 3. Build the user prompt with project context and RAG context
            var userPrompt = BuildBoqUserPrompt(request, ragResult);

            // 4. Send to AI Gateway
            var aiRequest = new AiCompletionRequest
            {
                TenantId = request.TenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                RagContext = ragResult?.FormattedContext,
                MaxTokensOverride = 8000,
                TemperatureOverride = 0.15 // Very low temperature for structured data consistency
            };

            var aiResponse = await _aiGateway.GenerateCompletionAsync(aiRequest, cancellationToken);

            stopwatch.Stop();

            if (!aiResponse.IsSuccess)
            {
                _logger.LogWarning(
                    "AI BOQ generation failed for project '{ProjectName}': {Error}",
                    request.ProjectNameAr, aiResponse.ErrorMessage);

                return Result.Failure<AiBoqGenerationResult>(
                    $"فشل توليد جدول الكميات: {aiResponse.ErrorMessage}");
            }

            // 5. Parse the structured response
            var parseResult = ParseBoqResponse(
                aiResponse.Content,
                aiResponse.Provider.ToString(),
                aiResponse.ModelName,
                stopwatch.ElapsedMilliseconds,
                ragResult);

            if (parseResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to parse AI BOQ response for project '{ProjectName}': {Error}",
                    request.ProjectNameAr, parseResult.Error);
                return parseResult;
            }

            _logger.LogInformation(
                "AI BOQ generation completed for project '{ProjectName}'. " +
                "Items: {ItemCount}, Grounding score: {Score}%, Latency: {LatencyMs}ms, " +
                "Tokens: {TotalTokens}",
                request.ProjectNameAr,
                parseResult.Value!.Items.Count,
                parseResult.Value.GroundingConfidenceScore,
                stopwatch.ElapsedMilliseconds,
                aiResponse.TotalTokens);

            return parseResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Unhandled exception during AI BOQ generation for project '{ProjectName}'",
                request.ProjectNameAr);

            return Result.Failure<AiBoqGenerationResult>(
                $"حدث خطأ غير متوقع أثناء توليد جدول الكميات: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<AiBoqGenerationResult>> RefineBoqAsync(
        AiBoqRefineRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting AI BOQ refinement for project '{ProjectName}' in competition {CompetitionId}",
                request.ProjectNameAr, request.CompetitionId);

            // 1. Retrieve relevant context
            var ragQuery = $"جدول كميات {request.ProjectNameAr} {request.UserFeedbackAr}";
            var ragResult = await RetrieveRagContextAsync(
                ragQuery, request.TenantId, request.CollectionName, cancellationToken);

            // 2. Build prompts
            var systemPrompt = BuildBoqRefineSystemPrompt();
            var userPrompt = BuildBoqRefineUserPrompt(request, ragResult);

            // 3. Send to AI Gateway
            var aiRequest = new AiCompletionRequest
            {
                TenantId = request.TenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                RagContext = ragResult?.FormattedContext,
                MaxTokensOverride = 8000,
                TemperatureOverride = 0.15
            };

            var aiResponse = await _aiGateway.GenerateCompletionAsync(aiRequest, cancellationToken);

            stopwatch.Stop();

            if (!aiResponse.IsSuccess)
            {
                _logger.LogWarning(
                    "AI BOQ refinement failed for project '{ProjectName}': {Error}",
                    request.ProjectNameAr, aiResponse.ErrorMessage);

                return Result.Failure<AiBoqGenerationResult>(
                    $"فشل تحسين جدول الكميات: {aiResponse.ErrorMessage}");
            }

            // 4. Parse response
            var parseResult = ParseBoqResponse(
                aiResponse.Content,
                aiResponse.Provider.ToString(),
                aiResponse.ModelName,
                stopwatch.ElapsedMilliseconds,
                ragResult);

            _logger.LogInformation(
                "AI BOQ refinement completed for project '{ProjectName}'. Latency: {LatencyMs}ms",
                request.ProjectNameAr, stopwatch.ElapsedMilliseconds);

            return parseResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Unhandled exception during AI BOQ refinement for project '{ProjectName}'",
                request.ProjectNameAr);

            return Result.Failure<AiBoqGenerationResult>(
                $"حدث خطأ غير متوقع أثناء تحسين جدول الكميات: {ex.Message}");
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
                    "RAG context retrieval for BOQ failed: {Error}. Proceeding without RAG context.",
                    result.ErrorMessage);
                return null;
            }

            _logger.LogInformation(
                "RAG context for BOQ retrieved. Chunks: {ChunkCount}, Time: {TimeMs}ms",
                result.Chunks.Count, result.RetrievalTimeMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "RAG context retrieval for BOQ threw an exception. Proceeding without RAG context.");
            return null;
        }
    }

    private static string BuildBoqRagQuery(AiBoqGenerationRequest request)
    {
        var sb = new StringBuilder();
        sb.Append($"جدول كميات {request.ProjectType} - {request.ProjectNameAr}");

        if (!string.IsNullOrWhiteSpace(request.SpecificationsContentHtml))
        {
            // Extract key terms from specifications for better RAG retrieval
            var plainText = StripHtmlTags(request.SpecificationsContentHtml);
            if (plainText.Length > 300)
                plainText = plainText[..300];
            sb.Append($" - {plainText}");
        }

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  BOQ Generation Prompt Construction
    // ═══════════════════════════════════════════════════════════════

    private static string BuildBoqSystemPrompt()
    {
        return """
            <role>
            أنت خبير متخصص في إعداد جداول الكميات (BOQ) للمشتريات الحكومية في المملكة العربية السعودية.
            لديك خبرة عميقة في:
            - إعداد جداول الكميات وفقاً لمعايير هيئة كفاءة الإنفاق والمشروعات الحكومية (EXPRO)
            - تحديد بنود الأعمال والكميات والوحدات المناسبة لمختلف أنواع المشاريع
            - تقدير التكاليف بناءً على البيانات التاريخية للمنافسات السابقة
            - نظام المنافسات والمشتريات الحكومية ولائحته التنفيذية
            أنت مساعد ذكي (Copilot) لفريق إعداد الكراسة، وليس صانع قرار. جميع مخرجاتك هي مسودات مقترحة تخضع لمراجعة واعتماد الفريق البشري.
            </role>

            <instructions>
            1. التزم باللغة العربية الفصحى في جميع الأوصاف والمبررات.
            2. استخدم الأرقام بالتنسيق الإنجليزي (0-9) ورمز الريال السعودي (SAR) للقيم المالية.
            3. اعتمد على المستندات المرجعية المقدمة في قسم <context> كأساس لتحديد البنود:
               - الخطوة الأولى: استخرج بنود الأعمال ذات الصلة من المستندات المرجعية والمنافسات السابقة.
               - الخطوة الثانية: ابنِ جدول الكميات بناءً على تلك المراجع فقط.
            4. قاعدة صارمة لمنع الهلوسة المالية:
               - لا تقدم أسعاراً تقديرية إلا إذا كانت مدعومة ببيانات تاريخية من المستندات المرجعية.
               - إذا لم تتوفر بيانات تاريخية للسعر، اترك حقل السعر التقديري فارغاً (null).
               - عند تقديم سعر تقديري، اذكر مصدره بوضوح (رقم المنافسة السابقة أو المستند المرجعي).
            5. رقّم البنود بشكل هرمي منطقي (مثل: 1.1، 1.2، 2.1).
            6. صنّف البنود في فئات واضحة (مثل: أعمال تقنية، أعمال استشارية، تراخيص، تدريب).
            7. اختر وحدات القياس المناسبة من القائمة المعتمدة:
               Each (وحدة)، LumpSum (مقطوعية)، SquareMeter (متر مربع)، LinearMeter (متر طولي)،
               CubicMeter (متر مكعب)، Kilogram (كيلوغرام)، Ton (طن)، Hour (ساعة)،
               Day (يوم)، Month (شهر)، Year (سنة)، Trip (رحلة)، Set (طقم)، Other (أخرى).
            8. قدم مبرراً واضحاً لكل بند يوضح سبب إدراجه مع الإشارة للمستند المرجعي.
            9. إذا لم تجد معلومات كافية لتحديد بند معين، صرّح بذلك في التحذيرات.
            10. أعد الإجابة بتنسيق JSON المحدد أدناه بدقة.
            </instructions>

            <examples>
            <example>
            مثال على بند في جدول كميات لمشروع تقنية معلومات:
            {
              "itemNumber": "1.1",
              "descriptionAr": "تحليل وتصميم النظام الإلكتروني",
              "descriptionEn": "System Analysis and Design",
              "unit": "LumpSum",
              "quantity": 1,
              "estimatedUnitPrice": 150000,
              "priceEstimateSource": "بناءً على منافسة سابقة رقم 2024/IT/015 لمشروع مماثل في وزارة التعليم",
              "category": "أعمال تقنية",
              "justificationAr": "يشمل هذا البند أعمال التحليل والتصميم الفني للنظام وفقاً للمتطلبات المحددة في المواصفات الفنية (القسم 3.1). المرجع: الدليل الاسترشادي لإعداد كراسات المشاريع الرقمية، الفصل 4.",
              "sortOrder": 1
            }
            </example>

            <example>
            مثال على بند بدون سعر تقديري (لعدم توفر بيانات تاريخية):
            {
              "itemNumber": "2.3",
              "descriptionAr": "تطوير واجهة برمجة التطبيقات (API) للربط مع الأنظمة الخارجية",
              "descriptionEn": "API Development for External System Integration",
              "unit": "Each",
              "quantity": 5,
              "estimatedUnitPrice": null,
              "priceEstimateSource": null,
              "category": "أعمال تقنية",
              "justificationAr": "مطلوب تطوير 5 واجهات برمجية للربط مع الأنظمة المحددة في المواصفات الفنية (القسم 3.4). لم تتوفر بيانات تاريخية كافية لتقدير السعر.",
              "sortOrder": 5
            }
            </example>

            <example>
            مثال على بند تدريب:
            {
              "itemNumber": "4.1",
              "descriptionAr": "تدريب المستخدمين النهائيين على استخدام النظام",
              "descriptionEn": "End-User Training on System Usage",
              "unit": "Day",
              "quantity": 10,
              "estimatedUnitPrice": 5000,
              "priceEstimateSource": "بناءً على متوسط أسعار التدريب في 3 منافسات سابقة مماثلة (2023/IT/008، 2024/IT/003، 2024/IT/012)",
              "category": "تدريب ونقل معرفة",
              "justificationAr": "يشمل تدريب 30 مستخدماً على مدار 10 أيام وفقاً لمتطلبات نقل المعرفة في المواصفات الفنية (القسم 6.2). المرجع: اللائحة التنفيذية، المادة 74 — وجوب تضمين بند التدريب.",
              "sortOrder": 10
            }
            </example>
            </examples>

            <output_format>
            أعد الإجابة بتنسيق JSON التالي بدقة (بدون أي نص إضافي خارج JSON):
            {
              "items": [
                {
                  "itemNumber": "1.1",
                  "descriptionAr": "وصف البند بالعربية",
                  "descriptionEn": "Item Description in English",
                  "unit": "LumpSum|Each|SquareMeter|...",
                  "quantity": 1,
                  "estimatedUnitPrice": 50000,
                  "priceEstimateSource": "مصدر التقدير أو null",
                  "category": "فئة البند",
                  "justificationAr": "مبرر إدراج البند مع المرجع",
                  "sortOrder": 1
                }
              ],
              "summaryAr": "ملخص جدول الكميات المقترح...",
              "totalEstimatedCost": 500000,
              "citations": [
                {
                  "documentName": "اسم المستند المرجعي",
                  "sectionReference": "رقم القسم أو المادة",
                  "pageNumbers": "أرقام الصفحات",
                  "quotedText": "النص المقتبس",
                  "usageContext": "كيف تم استخدام هذا المرجع"
                }
              ],
              "groundingConfidenceScore": 75.0,
              "warnings": ["أي تحذيرات حول بنود تحتاج مراجعة"]
            }
            </output_format>
            """;
    }

    private static string BuildBoqUserPrompt(
        AiBoqGenerationRequest request,
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

        if (!string.IsNullOrWhiteSpace(request.SpecificationsContentHtml))
        {
            sb.AppendLine("<specifications>");
            sb.AppendLine(request.SpecificationsContentHtml);
            sb.AppendLine("</specifications>");
        }

        if (!string.IsNullOrWhiteSpace(request.AdditionalInstructions))
        {
            sb.AppendLine($"<additional_instructions>{request.AdditionalInstructions}</additional_instructions>");
        }

        sb.AppendLine("</input>");

        sb.AppendLine();
        sb.AppendLine("المطلوب: قم بإنشاء جدول كميات (BOQ) شامل للمشروع المحدد أعلاه، مع الالتزام بالتعليمات والمستندات المرجعية المقدمة.");
        sb.AppendLine("تذكير صارم: لا تقدم أسعاراً تقديرية إلا إذا كانت مدعومة ببيانات تاريخية. إذا لم تجد المعلومة في السياق المقدم، صرّح بذلك بوضوح.");

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  BOQ Refinement Prompt Construction
    // ═══════════════════════════════════════════════════════════════

    private static string BuildBoqRefineSystemPrompt()
    {
        return """
            <role>
            أنت خبير متخصص في تحسين وتنقيح جداول الكميات (BOQ) للمشتريات الحكومية في المملكة العربية السعودية.
            مهمتك هي تحسين جدول الكميات الحالي بناءً على ملاحظات المستخدم مع الحفاظ على الدقة والتأريض.
            أنت مساعد ذكي (Copilot) لفريق إعداد الكراسة، وليس صانع قرار.
            </role>

            <instructions>
            1. التزم باللغة العربية الفصحى.
            2. حسّن جدول الكميات الحالي بناءً على ملاحظات المستخدم.
            3. حافظ على البنود والمراجع الصحيحة من الجدول الأصلي.
            4. لا تضف أسعاراً تقديرية جديدة دون سند من البيانات التاريخية.
            5. أعد الإجابة بنفس تنسيق JSON المستخدم في التوليد الأولي.
            </instructions>

            <output_format>
            أعد الإجابة بتنسيق JSON التالي بدقة (بدون أي نص إضافي خارج JSON):
            {
              "items": [...],
              "summaryAr": "...",
              "totalEstimatedCost": null,
              "citations": [...],
              "groundingConfidenceScore": 75.0,
              "warnings": [...]
            }
            </output_format>
            """;
    }

    private static string BuildBoqRefineUserPrompt(
        AiBoqRefineRequest request,
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
        sb.AppendLine();
        sb.AppendLine("<current_boq>");
        sb.AppendLine(request.ExistingBoqJson);
        sb.AppendLine("</current_boq>");
        sb.AppendLine();
        sb.AppendLine("<user_feedback>");
        sb.AppendLine(request.UserFeedbackAr);
        sb.AppendLine("</user_feedback>");
        sb.AppendLine("</input>");

        sb.AppendLine();
        sb.AppendLine("المطلوب: قم بتحسين جدول الكميات الحالي بناءً على ملاحظات المستخدم أعلاه.");

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Response Parsing
    // ═══════════════════════════════════════════════════════════════

    private static Result<AiBoqGenerationResult> ParseBoqResponse(
        string aiContent,
        string providerName,
        string modelName,
        long latencyMs,
        ContextRetrievalResult? ragResult)
    {
        try
        {
            var jsonContent = ExtractJsonFromResponse(aiContent);

            var parsed = JsonSerializer.Deserialize<BoqJsonResponse>(jsonContent, s_jsonOptions);

            if (parsed is null)
            {
                return Result.Failure<AiBoqGenerationResult>(
                    "فشل في تحليل استجابة الذكاء الاصطناعي: الاستجابة فارغة.");
            }

            var items = (parsed.Items ?? [])
                .Select(i => new GeneratedBoqItem
                {
                    ItemNumber = i.ItemNumber ?? "0",
                    DescriptionAr = i.DescriptionAr ?? "غير محدد",
                    DescriptionEn = i.DescriptionEn ?? "Not specified",
                    Unit = i.Unit ?? "Each",
                    Quantity = i.Quantity > 0 ? i.Quantity : 1,
                    EstimatedUnitPrice = i.EstimatedUnitPrice,
                    PriceEstimateSource = i.PriceEstimateSource,
                    Category = i.Category,
                    JustificationAr = i.JustificationAr ?? "",
                    SortOrder = i.SortOrder
                })
                .ToList();

            // Validate: prices must have sources (anti-hallucination)
            var warnings = new List<string>(parsed.Warnings ?? []);
            foreach (var item in items)
            {
                if (item.EstimatedUnitPrice.HasValue &&
                    string.IsNullOrWhiteSpace(item.PriceEstimateSource))
                {
                    warnings.Add(
                        $"تحذير: البند {item.ItemNumber} ({item.DescriptionAr}) يحتوي على سعر تقديري بدون مصدر بيانات تاريخية — يحتاج مراجعة.");
                }
            }

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

            // Enrich citations with RAG chunk metadata
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

            // Calculate total estimated cost only from items with prices
            decimal? totalCost = null;
            var itemsWithPrices = items.Where(i => i.EstimatedUnitPrice.HasValue).ToList();
            if (itemsWithPrices.Count > 0)
            {
                totalCost = itemsWithPrices.Sum(i => i.Quantity * i.EstimatedUnitPrice!.Value);
            }

            return Result.Success(new AiBoqGenerationResult
            {
                Items = items,
                SummaryAr = parsed.SummaryAr ?? "",
                TotalEstimatedCost = totalCost ?? parsed.TotalEstimatedCost,
                Citations = citations,
                GroundingConfidenceScore = parsed.GroundingConfidenceScore,
                Warnings = warnings,
                ProviderName = providerName,
                ModelName = modelName,
                LatencyMs = latencyMs
            });
        }
        catch (JsonException ex)
        {
            return Result.Failure<AiBoqGenerationResult>(
                $"فشل في تحليل استجابة JSON من الذكاء الاصطناعي: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Utility Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Extracts JSON content from AI response, handling potential markdown code blocks.
    /// </summary>
    private static string ExtractJsonFromResponse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "{}";

        var trimmed = content.Trim();

        // Handle markdown code blocks
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

        return trimmed;
    }

    /// <summary>
    /// Strips HTML tags from content to extract plain text for RAG queries.
    /// </summary>
    private static string StripHtmlTags(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Simple HTML tag removal for RAG query building
        var result = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
        return result.Trim();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Internal JSON Response Models (for deserialization only)
    // ═══════════════════════════════════════════════════════════════

    private sealed record BoqJsonResponse
    {
        public List<BoqItemJson>? Items { get; init; }
        public string? SummaryAr { get; init; }
        public decimal? TotalEstimatedCost { get; init; }
        public List<CitationJson>? Citations { get; init; }
        public double GroundingConfidenceScore { get; init; }
        public List<string>? Warnings { get; init; }
    }

    private sealed record BoqItemJson
    {
        public string? ItemNumber { get; init; }
        public string? DescriptionAr { get; init; }
        public string? DescriptionEn { get; init; }
        public string? Unit { get; init; }
        public decimal Quantity { get; init; }
        public decimal? EstimatedUnitPrice { get; init; }
        public string? PriceEstimateSource { get; init; }
        public string? Category { get; init; }
        public string? JustificationAr { get; init; }
        public int SortOrder { get; init; }
    }

    private sealed record CitationJson
    {
        public string? DocumentName { get; init; }
        public string? SectionReference { get; init; }
        public string? PageNumbers { get; init; }
        public string? QuotedText { get; init; }
        public string? UsageContext { get; init; }
    }
}
