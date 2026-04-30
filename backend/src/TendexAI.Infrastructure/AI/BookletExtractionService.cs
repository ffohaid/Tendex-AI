using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
/// - Includes retry logic with progressive text truncation for large documents
/// - Includes JSON repair for truncated AI responses
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

    /// <summary>Maximum characters to send to the AI in the first extraction attempt.</summary>
    private const int MaxDocumentCharsFirstAttempt = 45000;

    /// <summary>Maximum characters for retry attempts when the initial parse fails.</summary>
    private const int MaxDocumentCharsRetry = 25000;

    /// <summary>Upper bound for completion tokens to keep extraction within request time budgets.</summary>
    private const int MaxCompletionTokens = 6000;

    /// <summary>Maximum number of extraction attempts.</summary>
    private const int MaxAttempts = 2;

    private readonly IAiGateway _aiGateway;
    private readonly ILogger<BookletExtractionService> _logger;

    public BookletExtractionService(
        IAiGateway aiGateway,
        ILogger<BookletExtractionService> logger)
    {
        _aiGateway = aiGateway;
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
                "Starting booklet extraction from document '{FileName}' ({ContentType}, {Size} bytes, {TextLength} chars)",
                request.FileName, request.ContentType, request.FileSizeBytes, request.DocumentText?.Length ?? 0);

            // 1. Extraction must prioritize the uploaded document itself.
            // Retrieving RAG context here increases latency and token size while the task is
            // already anchored to the source document, so this path stays document-only.
            ContextRetrievalResult? ragResult = null;

            // 2. Attempt extraction with retry logic
            Result<BookletExtractionResult>? lastResult = null;

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                var maxChars = attempt == 1 ? MaxDocumentCharsFirstAttempt : MaxDocumentCharsRetry;
                var truncatedText = TruncateDocumentText(request.DocumentText, maxChars);

                _logger.LogInformation(
                    "Extraction attempt {Attempt}/{MaxAttempts}: sending {CharCount} chars (original: {OriginalChars})",
                    attempt, MaxAttempts, truncatedText.Length, request.DocumentText?.Length ?? 0);

                // Build prompts - extraction must remain verbatim-focused even on retries
                var wasTruncated = truncatedText.Length < (request.DocumentText?.Length ?? 0);
                var systemPrompt = BuildExtractionSystemPrompt(wasTruncated);
                var modifiedRequest = request with { DocumentText = truncatedText };
                var userPrompt = BuildExtractionUserPrompt(modifiedRequest, ragResult, wasTruncated);

                // Send to AI Gateway
                var aiRequest = new AiCompletionRequest
                {
                    TenantId = request.TenantId,
                    SystemPrompt = systemPrompt,
                    UserPrompt = userPrompt,
                    RagContext = null,
                    MaxTokensOverride = MaxCompletionTokens,
                    TemperatureOverride = 0.1 // Very low temperature for structured extraction
                };

                var aiResponse = await _aiGateway.GenerateCompletionAsync(aiRequest, cancellationToken);

                if (!aiResponse.IsSuccess)
                {
                    _logger.LogWarning(
                        "AI booklet extraction attempt {Attempt} failed for '{FileName}': {Error}",
                        attempt, request.FileName, aiResponse.ErrorMessage);

                    lastResult = Result.Failure<BookletExtractionResult>(
                        $"فشل استخراج محتوى الكراسة: {aiResponse.ErrorMessage}");
                    continue;
                }

                // Parse the structured response
                var parseResult = ParseExtractionResponse(
                    aiResponse.Content,
                    aiResponse.Provider.ToString(),
                    aiResponse.ModelName,
                    stopwatch.ElapsedMilliseconds);

                if (parseResult.IsSuccess)
                {
                    _logger.LogInformation(
                        "Booklet extraction completed for '{FileName}' on attempt {Attempt}: " +
                        "{SectionCount} sections, {BoqCount} BOQ items, confidence: {Confidence}%, latency: {LatencyMs}ms",
                        request.FileName, attempt,
                        parseResult.Value!.Sections.Count,
                        parseResult.Value.BoqItems.Count,
                        parseResult.Value.ConfidenceScore,
                        stopwatch.ElapsedMilliseconds);

                    return parseResult;
                }

                _logger.LogWarning(
                    "Parse failed on attempt {Attempt} for '{FileName}': {Error}. Will retry with shorter text.",
                    attempt, request.FileName, parseResult.Error);

                lastResult = parseResult;
            }

            stopwatch.Stop();
            return lastResult ?? Result.Failure<BookletExtractionResult>(
                "فشل استخراج المحتوى بعد جميع المحاولات.");
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
    //  Text Truncation
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Truncates document text to a maximum character count, preserving
    /// the beginning and end of the document (which typically contain
    /// the most important structural information).
    /// </summary>
    private static string TruncateDocumentText(string? text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (text.Length <= maxChars)
            return text;

        // Take 70% from the beginning and 30% from the end
        var headChars = (int)(maxChars * 0.7);
        var tailChars = maxChars - headChars - 200; // 200 chars for the separator

        var head = text[..headChars];
        var tail = text[^tailChars..];

        return $"{head}\n\n[... تم اقتطاع {text.Length - headChars - tailChars} حرف من وسط المستند لتقليل الحجم ...]\n\n{tail}";
    }

    // ═══════════════════════════════════════════════════════════════
    //  Prompt Construction
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Builds the system prompt for document extraction.
    /// </summary>
    private static string BuildExtractionSystemPrompt(bool isTruncated = false)
    {
        var contentInstruction = isTruncated
            ? """
              6. في حقل contentHtml: انقل النص الموجود في المصدر **حرفياً قدر الإمكان** مع الحفاظ على ترتيب الفقرات والقوائم والعناوين الفرعية.
                 إذا كان النص طويلًا جداً فاستخدم مقتطفات حرفية متتابعة من المصدر ولا تكتب ملخصاً أو إعادة صياغة.
                 استخدم عناصر HTML بسيطة فقط: <p>, <ul>, <ol>, <li>, <strong>, <h3>, <h4>, <table>, <tr>, <td>.
              """
            : """
              6. في حقل contentHtml: انقل النص الموجود في المصدر **حرفياً قدر الإمكان** مع الحفاظ على ترتيب الفقرات والقوائم والعناوين الفرعية.
                 يُمنع التلخيص أو إعادة الصياغة أو استبدال المصطلحات أو اختراع محتوى غير موجود.
                 استخدم عناصر HTML بسيطة فقط: <p>, <ul>, <ol>, <li>, <strong>, <h3>, <h4>, <table>, <tr>, <td>.
              """;

        var jsonTemplate = """
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
                  "contentHtml": "<p>نص منقول من المصدر كما ورد مع الحفاظ على البنية</p>",
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
            """;

        return $"""
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
               - العنوان كما ورد في المصدر دون تعديل لغوي أو تلخيص أو تصحيح تلقائي
               - العنوان بالإنجليزية فقط إذا كان موجوداً صراحة في المصدر
               - نوع القسم (generalInformation, technicalSpecifications, termsAndConditions, evaluationCriteria, billOfQuantities, attachments, custom)
               - المحتوى بتنسيق HTML مع الحفاظ على النص الفعلي والبنية الأصلية قدر الإمكان
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
            5. إذا لم تتمكن من تحديد معلومة معينة، اتركها فارغة أو null ولا تخمن أبداً.
            6. العناوين القانونية وأسماء الأقسام والمواد والنسب المئوية والأرقام المرجعية والتواريخ يجب أن تُنسخ كما وردت في المصدر دون استنتاج.
            {contentInstruction}
            7. قدّر مستوى الثقة (0-100) لجودة الاستخراج الكلية.
            8. أضف تحذيرات فقط عند وجود اقتطاع فعلي للنص، أو نص غير مقروء، أو أقسام لم يمكن تحديدها بثقة.

            ⚠️ مهم جداً: يجب أن يكون الرد JSON صالحاً ومكتملاً. لا تقطع الرد في المنتصف.
            إذا كان المحتوى كثيراً، فحافظ على النقل الحرفي لأهم المقاطع الظاهرة في النص المتاح ولا تكتب أي تلخيص إنشائي.
            </instructions>

            <output_format>
            أعد الإجابة بتنسيق JSON التالي بدقة (بدون أي نص إضافي خارج JSON):
            {jsonTemplate}
            </output_format>
            """;
    }

    /// <summary>
    /// Builds the user prompt with the document text and metadata.
    /// Per RAG Guidelines Section 3.3: data at top, instructions at bottom.
    /// </summary>
    private static string BuildExtractionUserPrompt(
        BookletExtractionRequest request,
        ContextRetrievalResult? ragResult,
        bool isSummarized = false)
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

        if (isSummarized)
        {
            sb.AppendLine("⚠️ تم اقتطاع جزء من النص بسبب حدود المعالجة. اعمل فقط على النص الظاهر، ولا تذكر تحذير الحجم إلا إذا كان الاقتطاع واضحاً فعلاً داخل النص.");
        }

        sb.AppendLine("انقل محتوى الأقسام والعناوين كما ورد في النص الظاهر. لا تلخّص ولا تعِد الصياغة ولا تستبدل أسماء الأقسام بمرادفات.");

        sb.AppendLine("⚠️ تأكد أن JSON مكتمل وصالح. لا تقطع الرد.");
        sb.AppendLine("</task>");

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Response Parsing
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Parses the AI response into a structured BookletExtractionResult.
    /// Includes JSON repair for truncated responses.
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

            // Try parsing as-is first
            ExtractionJsonResponse? parsed = null;

            try
            {
                parsed = JsonSerializer.Deserialize<ExtractionJsonResponse>(jsonContent, s_jsonOptions);
            }
            catch (JsonException)
            {
                // Try to repair truncated JSON
                _logger.LogWarning("Initial JSON parse failed, attempting repair...");
                var repairedJson = RepairTruncatedJson(jsonContent);

                try
                {
                    parsed = JsonSerializer.Deserialize<ExtractionJsonResponse>(repairedJson, s_jsonOptions);
                    _logger.LogInformation("JSON repair successful");
                }
                catch (JsonException repairEx)
                {
                    _logger.LogWarning(repairEx, "JSON repair also failed, attempting minimal extraction...");

                    // Last resort: try to extract what we can from partial JSON
                    var minimalResult = ExtractFromPartialJson(jsonContent, providerName, modelName, latencyMs);
                    if (minimalResult is not null)
                    {
                        _logger.LogInformation("Minimal extraction from partial JSON succeeded");
                        return Result.Success(minimalResult);
                    }

                    throw; // Re-throw if even minimal extraction fails
                }
            }

            if (parsed is null)
            {
                return Result.Failure<BookletExtractionResult>(
                    "فشل تحليل استجابة الذكاء الاصطناعي: النتيجة فارغة.");
            }

            return MapParsedResponse(parsed, providerName, modelName, latencyMs);
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
    /// Maps a parsed JSON response to a BookletExtractionResult.
    /// </summary>
    private static Result<BookletExtractionResult> MapParsedResponse(
        ExtractionJsonResponse parsed,
        string providerName,
        string modelName,
        long latencyMs)
    {
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

    // ═══════════════════════════════════════════════════════════════
    //  JSON Repair Utilities
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Attempts to repair truncated JSON by closing open brackets and braces.
    /// </summary>
    private static string RepairTruncatedJson(string json)
    {
        var sb = new StringBuilder(json);

        // Track open brackets/braces
        var stack = new Stack<char>();
        bool inString = false;
        bool escaped = false;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (c == '"' && !escaped)
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            switch (c)
            {
                case '{':
                    stack.Push('}');
                    break;
                case '[':
                    stack.Push(']');
                    break;
                case '}':
                    if (stack.Count > 0 && stack.Peek() == '}')
                        stack.Pop();
                    break;
                case ']':
                    if (stack.Count > 0 && stack.Peek() == ']')
                        stack.Pop();
                    break;
            }
        }

        // If we're inside a string, close it
        if (inString)
        {
            // Find the last complete property and truncate there
            var lastGoodPos = FindLastCompleteProperty(json);
            if (lastGoodPos > 0)
            {
                sb = new StringBuilder(json[..lastGoodPos]);
                // Recount the stack
                stack.Clear();
                inString = false;
                escaped = false;
                for (int i = 0; i < lastGoodPos; i++)
                {
                    char c = json[i];
                    if (escaped) { escaped = false; continue; }
                    if (c == '\\' && inString) { escaped = true; continue; }
                    if (c == '"' && !escaped) { inString = !inString; continue; }
                    if (inString) continue;
                    switch (c)
                    {
                        case '{': stack.Push('}'); break;
                        case '[': stack.Push(']'); break;
                        case '}': if (stack.Count > 0 && stack.Peek() == '}') stack.Pop(); break;
                        case ']': if (stack.Count > 0 && stack.Peek() == ']') stack.Pop(); break;
                    }
                }
            }
            else
            {
                sb.Append('"');
            }
        }

        // Close all open brackets/braces
        while (stack.Count > 0)
        {
            sb.Append(stack.Pop());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Finds the position of the last complete JSON property (after a comma or opening brace/bracket).
    /// </summary>
    private static int FindLastCompleteProperty(string json)
    {
        // Look for the last comma that's not inside a string
        bool inString = false;
        bool escaped = false;
        int lastComma = -1;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            if (escaped) { escaped = false; continue; }
            if (c == '\\' && inString) { escaped = true; continue; }
            if (c == '"' && !escaped) { inString = !inString; continue; }
            if (inString) continue;
            if (c == ',') lastComma = i;
        }

        return lastComma > 0 ? lastComma : -1;
    }

    /// <summary>
    /// Attempts to extract basic information from a partial/broken JSON response
    /// using regex patterns.
    /// </summary>
    private BookletExtractionResult? ExtractFromPartialJson(
        string json,
        string providerName,
        string modelName,
        long latencyMs)
    {
        try
        {
            // Extract project name
            var nameMatch = Regex.Match(json, @"""projectNameAr""\s*:\s*""([^""]+)""");
            var projectName = nameMatch.Success ? nameMatch.Groups[1].Value : "مشروع بدون عنوان";

            var nameEnMatch = Regex.Match(json, @"""projectNameEn""\s*:\s*""([^""]+)""");
            var projectNameEn = nameEnMatch.Success ? nameEnMatch.Groups[1].Value : null;

            var descMatch = Regex.Match(json, @"""projectDescription""\s*:\s*""([^""]+)""");
            var description = descMatch.Success ? descMatch.Groups[1].Value : null;

            // Extract sections using regex
            var sections = new List<ExtractedSection>();
            var sectionMatches = Regex.Matches(json,
                @"\{\s*""titleAr""\s*:\s*""([^""]*)""\s*,\s*""titleEn""\s*:\s*""([^""]*)""\s*,\s*""sectionType""\s*:\s*""([^""]*)""",
                RegexOptions.None, TimeSpan.FromSeconds(5));

            var sortOrder = 1;
            foreach (Match m in sectionMatches)
            {
                sections.Add(new ExtractedSection
                {
                    TitleAr = m.Groups[1].Value,
                    TitleEn = m.Groups[2].Value,
                    SectionType = MapSectionType(m.Groups[3].Value),
                    ContentHtml = "<p>تم استخراج العنوان فقط - يرجى مراجعة المحتوى</p>",
                    IsMandatory = true,
                    SortOrder = sortOrder++,
                    ConfidenceScore = 30.0
                });
            }

            if (sections.Count == 0)
                return null; // Can't extract anything useful

            return new BookletExtractionResult
            {
                ProjectNameAr = projectName,
                ProjectNameEn = projectNameEn,
                ProjectDescription = description,
                DetectedCompetitionType = null,
                EstimatedBudget = null,
                ProjectDurationDays = null,
                Sections = sections,
                BoqItems = [],
                ExtractionSummaryAr = "تم التقاط جزء من بيانات الاستخراج بعد تعذر تحليل الاستجابة كاملة. يرجى مراجعة الأقسام المستخرجة بعناية.",
                ConfidenceScore = 30.0,
                Warnings =
                [
                    "تمت استعادة جزء من الاستجابة فقط بعد فشل تحليل JSON الكامل",
                    "يرجى مراجعة محتوى كل قسم والتأكد من اكتمال النص المنقول من المصدر"
                ],
                ProviderName = providerName,
                ModelName = modelName,
                LatencyMs = latencyMs
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract from partial JSON");
            return null;
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
        else if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed[3..];
        }

        if (trimmed.EndsWith("```", StringComparison.Ordinal))
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
