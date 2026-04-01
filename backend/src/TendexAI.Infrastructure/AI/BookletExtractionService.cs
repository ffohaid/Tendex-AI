using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.AI;

/// <summary>
/// Infrastructure implementation of <see cref="IBookletExtractionService"/>.
/// Uses the unified AI Gateway with RAG context to extract structured booklet content
/// from uploaded documents (PDF/Word).
///
/// This powers the "Upload &amp; Extract" (رفع واستخراج) creation method.
///
/// Architecture:
/// - Receives raw text extracted from the uploaded document
/// - Constructs a structured Arabic prompt for the AI to parse the document
/// - AI identifies sections, BOQ items, and project metadata
/// - Parses the structured JSON response into domain models
///
/// Per RAG Guidelines:
/// - Section 2.1: Arabic language sovereignty
/// - Section 3.1: Mandatory XML prompt structure
/// - Section 3.3: Long Context Management (data at top, instructions at bottom)
/// - Section 5.1: Booklet quality standards
/// </summary>
public sealed class BookletExtractionService : IBookletExtractionService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            | System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private readonly IAiGateway _aiGateway;
    private readonly IContextRetrievalService _contextRetrievalService;
    private readonly ILogger<BookletExtractionService> _logger;

    public BookletExtractionService(
        IAiGateway aiGateway,
        IContextRetrievalService contextRetrievalService,
        ILogger<BookletExtractionService> logger)
    {
        _aiGateway = aiGateway;
        _contextRetrievalService = contextRetrievalService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<BookletExtractionResult>> ExtractBookletAsync(
        BookletExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting booklet extraction from document '{FileName}' ({ContentType}, {Size} bytes)",
                request.FileName, request.ContentType, request.FileSizeBytes);

            // 1. Optionally retrieve RAG context for better extraction quality
            ContextRetrievalResult? ragResult = null;
            try
            {
                var ragQuery = "كراسة شروط ومواصفات هيكل أقسام جدول كميات";
                var retrievalRequest = new ContextRetrievalRequest
                {
                    Query = ragQuery,
                    TenantId = request.TenantId,
                    CollectionName = request.CollectionName,
                    TopK = 3,
                    InitialCandidates = 10,
                    ScoreThreshold = 0.5f
                };

                ragResult = await _contextRetrievalService.RetrieveContextAsync(
                    retrievalRequest, cancellationToken);

                if (ragResult is not null && !ragResult.IsSuccess)
                {
                    _logger.LogWarning("RAG retrieval failed, proceeding without context");
                    ragResult = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RAG retrieval threw exception, proceeding without context");
            }

            // 2. Build the extraction prompts
            var systemPrompt = BuildExtractionSystemPrompt();
            var userPrompt = BuildExtractionUserPrompt(request, ragResult);

            // 3. Send to AI Gateway
            var aiRequest = new AiCompletionRequest
            {
                TenantId = request.TenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                RagContext = ragResult?.FormattedContext,
                MaxTokensOverride = 16000,
                TemperatureOverride = 0.1 // Very low temperature for structured extraction
            };

            var aiResponse = await _aiGateway.GenerateCompletionAsync(aiRequest, cancellationToken);

            stopwatch.Stop();

            if (!aiResponse.IsSuccess)
            {
                _logger.LogWarning(
                    "AI booklet extraction failed for '{FileName}': {Error}",
                    request.FileName, aiResponse.ErrorMessage);

                return Result.Failure<BookletExtractionResult>(
                    $"فشل استخراج محتوى الكراسة: {aiResponse.ErrorMessage}");
            }

            // 4. Parse the structured response
            var parseResult = ParseExtractionResponse(
                aiResponse.Content,
                aiResponse.Provider.ToString(),
                aiResponse.ModelName,
                stopwatch.ElapsedMilliseconds);

            if (parseResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to parse AI extraction response for '{FileName}': {Error}",
                    request.FileName, parseResult.Error);
                return parseResult;
            }

            _logger.LogInformation(
                "Booklet extraction completed for '{FileName}': {SectionCount} sections, " +
                "{BoqCount} BOQ items, confidence: {Confidence}%, latency: {LatencyMs}ms",
                request.FileName,
                parseResult.Value!.Sections.Count,
                parseResult.Value.BoqItems.Count,
                parseResult.Value.ConfidenceScore,
                stopwatch.ElapsedMilliseconds);

            return parseResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Unhandled exception during booklet extraction for '{FileName}'",
                request.FileName);

            return Result.Failure<BookletExtractionResult>(
                $"حدث خطأ غير متوقع أثناء استخراج المحتوى: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Prompt Construction
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds the system prompt for document extraction.
    /// </summary>
    private static string BuildExtractionSystemPrompt()
    {
        return """
            <role>
            أنت خبير متخصص في تحليل واستخراج محتوى كراسات الشروط والمواصفات للمشتريات الحكومية في المملكة العربية السعودية.
            مهمتك هي تحليل نص مستند مرفوع (كراسة شروط أو مواصفات فنية) واستخراج هيكله ومحتواه بشكل منظم.
            لديك خبرة عميقة في:
            - تحليل كراسات الشروط والمواصفات وفقاً لنظام المنافسات والمشتريات الحكومية
            - التعرف على أقسام الكراسة (معلومات عامة، مواصفات فنية، شروط وأحكام، معايير تقييم، جدول كميات)
            - استخراج البيانات المهيكلة من النصوص غير المهيكلة
            - التعامل مع المستندات العربية والثنائية اللغة
            </role>

            <instructions>
            1. حلل النص المقدم في قسم <document_text> واستخرج منه:
               أ. اسم المشروع (بالعربية والإنجليزية إن وُجد)
               ب. وصف المشروع
               ج. نوع المنافسة (عامة، محدودة، شراء مباشر، إطارية)
               د. الميزانية التقديرية (إن وُجدت)
               هـ. مدة المشروع بالأيام (إن وُجدت)
               و. أقسام الكراسة مع محتواها
               ز. بنود جدول الكميات (إن وُجدت)

            2. لكل قسم مستخرج، حدد:
               - العنوان بالعربية (والإنجليزية إن أمكن)
               - نوع القسم (generalInformation, technicalSpecifications, termsAndConditions, evaluationCriteria, billOfQuantities, attachments, custom)
               - المحتوى بتنسيق HTML نظيف
               - هل القسم إلزامي أم اختياري
               - ترتيب العرض

            3. لبنود جدول الكميات، استخرج:
               - رقم البند
               - الوصف
               - الوحدة
               - الكمية
               - السعر التقديري (إن وُجد)
               - التصنيف

            4. استخدم الأرقام بالتنسيق الإنجليزي (0-9) حصراً.
            5. حافظ على النص الأصلي قدر الإمكان دون إعادة صياغة.
            6. إذا لم تتمكن من تحديد معلومة معينة، اتركها فارغة ولا تخمن.
            7. قدّر مستوى الثقة (0-100) لجودة الاستخراج الكلية.
            8. أضف تحذيرات لأي أقسام غير واضحة أو ناقصة.
            </instructions>

            <output_format>
            أعد الإجابة بتنسيق JSON التالي بدقة (بدون أي نص إضافي خارج JSON):
            {
              "projectNameAr": "اسم المشروع بالعربية",
              "projectNameEn": "Project Name in English (or null)",
              "projectDescription": "وصف المشروع",
              "detectedCompetitionType": "public|limited|directPurchase|framework|null",
              "estimatedBudget": 0.0,
              "projectDurationDays": 0,
              "sections": [
                {
                  "titleAr": "عنوان القسم بالعربية",
                  "titleEn": "Section Title in English",
                  "sectionType": "generalInformation|technicalSpecifications|termsAndConditions|evaluationCriteria|billOfQuantities|attachments|custom",
                  "contentHtml": "<p>محتوى القسم بتنسيق HTML</p>",
                  "isMandatory": true,
                  "sortOrder": 1,
                  "confidenceScore": 85.0
                }
              ],
              "boqItems": [
                {
                  "itemNumber": "1",
                  "descriptionAr": "وصف البند",
                  "unit": "وحدة",
                  "quantity": 1.0,
                  "estimatedUnitPrice": 0.0,
                  "category": "تصنيف",
                  "sortOrder": 1
                }
              ],
              "extractionSummaryAr": "ملخص عملية الاستخراج",
              "confidenceScore": 85.0,
              "warnings": ["أي تحذيرات"]
            }
            </output_format>
            """;
    }

    /// <summary>
    /// Builds the user prompt with the document text and metadata.
    /// Per RAG Guidelines Section 3.3: data at top, instructions at bottom.
    /// </summary>
    private static string BuildExtractionUserPrompt(
        BookletExtractionRequest request,
        ContextRetrievalResult? ragResult)
    {
        var sb = new StringBuilder();

        // RAG context at the top (if available)
        if (ragResult?.FormattedContext is not null)
        {
            sb.AppendLine("<reference_context>");
            sb.AppendLine(ragResult.FormattedContext);
            sb.AppendLine("</reference_context>");
            sb.AppendLine();
        }

        // Document metadata
        sb.AppendLine("<document_metadata>");
        sb.AppendLine($"<file_name>{request.FileName}</file_name>");
        sb.AppendLine($"<content_type>{request.ContentType}</content_type>");
        sb.AppendLine($"<file_size_bytes>{request.FileSizeBytes}</file_size_bytes>");
        if (request.PageCount.HasValue)
        {
            sb.AppendLine($"<page_count>{request.PageCount.Value}</page_count>");
        }
        sb.AppendLine("</document_metadata>");
        sb.AppendLine();

        // Document text (the main content to analyze)
        sb.AppendLine("<document_text>");
        sb.AppendLine(request.DocumentText);
        sb.AppendLine("</document_text>");
        sb.AppendLine();

        // Final instruction
        sb.AppendLine("<task>");
        sb.AppendLine("حلل النص أعلاه واستخرج هيكل كراسة الشروط والمواصفات بالكامل.");
        sb.AppendLine("أعد النتيجة بتنسيق JSON المحدد في output_format.");
        sb.AppendLine("تأكد من استخراج جميع الأقسام والبنود الموجودة في المستند.");
        sb.AppendLine("حافظ على النص الأصلي قدر الإمكان في حقل contentHtml.");
        sb.AppendLine("</task>");

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Response Parsing
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Parses the AI response into a structured BookletExtractionResult.
    /// </summary>
    private Result<BookletExtractionResult> ParseExtractionResponse(
        string aiContent,
        string providerName,
        string modelName,
        long latencyMs)
    {
        try
        {
            // Clean the response - remove markdown code fences if present
            var jsonContent = CleanJsonResponse(aiContent);

            var parsed = JsonSerializer.Deserialize<ExtractionJsonResponse>(jsonContent, s_jsonOptions);

            if (parsed is null)
            {
                return Result.Failure<BookletExtractionResult>(
                    "فشل تحليل استجابة الذكاء الاصطناعي: النتيجة فارغة.");
            }

            // Map sections
            var sections = new List<ExtractedSection>();
            if (parsed.Sections is not null)
            {
                var sortOrder = 1;
                foreach (var s in parsed.Sections)
                {
                    sections.Add(new ExtractedSection
                    {
                        TitleAr = s.TitleAr ?? $"قسم {sortOrder}",
                        TitleEn = s.TitleEn ?? "",
                        SectionType = MapSectionType(s.SectionType),
                        ContentHtml = s.ContentHtml ?? "",
                        IsMandatory = s.IsMandatory,
                        SortOrder = s.SortOrder > 0 ? s.SortOrder : sortOrder,
                        ConfidenceScore = s.ConfidenceScore
                    });
                    sortOrder++;
                }
            }

            // Map BOQ items
            var boqItems = new List<ExtractedBoqItem>();
            if (parsed.BoqItems is not null)
            {
                var sortOrder = 1;
                foreach (var b in parsed.BoqItems)
                {
                    boqItems.Add(new ExtractedBoqItem
                    {
                        ItemNumber = b.ItemNumber ?? sortOrder.ToString(),
                        DescriptionAr = b.DescriptionAr ?? "",
                        Unit = b.Unit ?? "وحدة",
                        Quantity = b.Quantity > 0 ? b.Quantity : 1,
                        EstimatedUnitPrice = b.EstimatedUnitPrice,
                        Category = b.Category,
                        SortOrder = b.SortOrder > 0 ? b.SortOrder : sortOrder
                    });
                    sortOrder++;
                }
            }

            // Map competition type
            CompetitionType? detectedType = parsed.DetectedCompetitionType?.ToLowerInvariant() switch
            {
                "public" or "publictender" or "public_tender" => CompetitionType.PublicTender,
                "limited" or "limitedtender" or "limited_tender" => CompetitionType.LimitedTender,
                "directpurchase" or "direct_purchase" => CompetitionType.DirectPurchase,
                "framework" or "frameworkagreement" or "framework_agreement" => CompetitionType.FrameworkAgreement,
                "twostage" or "two_stage" => CompetitionType.TwoStageTender,
                "reverseauction" or "reverse_auction" => CompetitionType.ReverseAuction,
                _ => null
            };

            var result = new BookletExtractionResult
            {
                ProjectNameAr = parsed.ProjectNameAr ?? "مشروع بدون عنوان",
                ProjectNameEn = parsed.ProjectNameEn,
                ProjectDescription = parsed.ProjectDescription,
                DetectedCompetitionType = detectedType,
                EstimatedBudget = parsed.EstimatedBudget > 0 ? parsed.EstimatedBudget : null,
                ProjectDurationDays = parsed.ProjectDurationDays > 0 ? parsed.ProjectDurationDays : null,
                Sections = sections,
                BoqItems = boqItems,
                ExtractionSummaryAr = parsed.ExtractionSummaryAr ?? "تم استخراج محتوى الكراسة بنجاح.",
                ConfidenceScore = parsed.ConfidenceScore > 0 ? parsed.ConfidenceScore : 50.0,
                Warnings = parsed.Warnings ?? [],
                ProviderName = providerName,
                ModelName = modelName,
                LatencyMs = latencyMs
            };

            return Result.Success(result);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI extraction response as JSON");
            return Result.Failure<BookletExtractionResult>(
                $"فشل تحليل استجابة الذكاء الاصطناعي: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing AI extraction response");
            return Result.Failure<BookletExtractionResult>(
                $"خطأ غير متوقع أثناء تحليل الاستجابة: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans the AI response by removing markdown code fences and extra whitespace.
    /// </summary>
    private static string CleanJsonResponse(string content)
    {
        var trimmed = content.Trim();

        // Remove ```json ... ``` wrapper
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["```json".Length..];
        }
        else if (trimmed.StartsWith("```"))
        {
            trimmed = trimmed[3..];
        }

        if (trimmed.EndsWith("```"))
        {
            trimmed = trimmed[..^3];
        }

        return trimmed.Trim();
    }

    /// <summary>
    /// Maps a section type string from AI response to the RfpSectionType enum.
    /// </summary>
    private static RfpSectionType MapSectionType(string? sectionType)
    {
        return sectionType?.ToLowerInvariant() switch
        {
            "generalinformation" or "general_information" => RfpSectionType.GeneralInformation,
            "technicalspecifications" or "technical_specifications" => RfpSectionType.TechnicalSpecifications,
            "termsandconditions" or "terms_and_conditions" => RfpSectionType.TermsAndConditions,
            "evaluationcriteria" or "evaluation_criteria" => RfpSectionType.EvaluationCriteria,
            "billofquantities" or "bill_of_quantities" => RfpSectionType.BillOfQuantities,
            "attachments" => RfpSectionType.Attachments,
            _ => RfpSectionType.Custom
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Internal JSON Models for AI Response Parsing
    // ═══════════════════════════════════════════════════════════════

    private sealed record ExtractionJsonResponse
    {
        public string? ProjectNameAr { get; init; }
        public string? ProjectNameEn { get; init; }
        public string? ProjectDescription { get; init; }
        public string? DetectedCompetitionType { get; init; }
        public decimal? EstimatedBudget { get; init; }
        public int? ProjectDurationDays { get; init; }
        public List<SectionJsonResponse>? Sections { get; init; }
        public List<BoqItemJsonResponse>? BoqItems { get; init; }
        public string? ExtractionSummaryAr { get; init; }
        public double ConfidenceScore { get; init; }
        public List<string>? Warnings { get; init; }
    }

    private sealed record SectionJsonResponse
    {
        public string? TitleAr { get; init; }
        public string? TitleEn { get; init; }
        public string? SectionType { get; init; }
        public string? ContentHtml { get; init; }
        public bool IsMandatory { get; init; }
        public int SortOrder { get; init; }
        public double ConfidenceScore { get; init; }
    }

    private sealed record BoqItemJsonResponse
    {
        public string? ItemNumber { get; init; }
        public string? DescriptionAr { get; init; }
        public string? Unit { get; init; }
        public decimal Quantity { get; init; }
        public decimal? EstimatedUnitPrice { get; init; }
        public string? Category { get; init; }
        public int SortOrder { get; init; }
    }
}
