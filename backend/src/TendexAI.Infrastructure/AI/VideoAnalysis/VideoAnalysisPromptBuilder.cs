namespace TendexAI.Infrastructure.AI.VideoAnalysis;

/// <summary>
/// Builds structured prompts for the AI model to analyze video integrity.
/// All prompts are in Arabic as per platform requirements.
/// </summary>
internal static class VideoAnalysisPromptBuilder
{
    /// <summary>
    /// System prompt that establishes the AI's role as a video forensics expert.
    /// </summary>
    public static string BuildSystemPrompt()
    {
        return """
            أنت خبير متخصص في التحليل الجنائي للفيديو والتحقق من مصداقية التسجيلات المرئية.
            مهمتك هي تحليل التسجيلات المرئية المقدمة وتقييمها من ثلاثة محاور رئيسية:

            1. **التحقق من أصالة التسجيل**: هل التسجيل أصلي ومباشر أم أنه إعادة تسجيل من شاشة أخرى (هاتف، تلفزيون، حاسوب)؟
               - ابحث عن أنماط موجات موريه (Moiré patterns)
               - ابحث عن خطوط التحديث (refresh lines)
               - ابحث عن حواف الشاشة أو الإطارات المرئية
               - تحقق من جودة الصورة وتناسق الإضاءة
               - تحقق من وجود انعكاسات غير طبيعية

            2. **اكتشاف التلاعب**: هل تم تعديل أو قص أو دمج التسجيل؟
               - ابحث عن قطع مفاجئة في التسجيل
               - تحقق من تناسق الإضاءة والظلال عبر المشاهد
               - ابحث عن مؤشرات التزييف العميق (Deepfake)
               - تحقق من تناسق البيانات الوصفية (Metadata)

            3. **التحقق من الهوية**: هل الشخص الظاهر في التسجيل هو الشخص المتوقع؟
               - تحقق من وجود وجه واضح في التسجيل
               - قيّم جودة صورة الوجه (الوضوح، الإضاءة، الزاوية)
               - إذا توفرت صورة مرجعية، قارن بينها وبين الوجه في التسجيل

            يجب أن تكون إجاباتك:
            - موضوعية وعلمية
            - مبنية على أدلة مرئية واضحة
            - باللغة العربية الفصحى
            - بصيغة JSON المحددة

            مهم جداً: لا تصدر أحكاماً نهائية قاطعة إذا لم تكن الأدلة كافية. استخدم "غير حاسم" عند الضرورة.
            """;
    }

    /// <summary>
    /// Builds the user prompt for analyzing a specific video.
    /// </summary>
    public static string BuildAnalysisPrompt(
        string videoFileReference,
        string? videoFileName,
        long? fileSizeBytes,
        TimeSpan? duration,
        bool hasReferenceImage)
    {
        var durationText = duration.HasValue
            ? $"{duration.Value.TotalSeconds:F1} ثانية"
            : "غير محدد";

        var fileSizeText = fileSizeBytes.HasValue
            ? $"{fileSizeBytes.Value / (1024.0 * 1024.0):F2} ميجابايت"
            : "غير محدد";

        var referenceImageText = hasReferenceImage
            ? "نعم، تم توفير صورة مرجعية للمقارنة."
            : "لا، لم يتم توفير صورة مرجعية. قيّم جودة الوجه فقط.";

        return $$"""
            قم بتحليل التسجيل المرئي التالي وتقييم مصداقيته:

            **معلومات التسجيل:**
            - مرجع الملف: {{videoFileReference}}
            - اسم الملف: {{videoFileName ?? "غير محدد"}}
            - حجم الملف: {{fileSizeText}}
            - مدة التسجيل: {{durationText}}
            - صورة مرجعية للتحقق من الهوية: {{referenceImageText}}

            **المطلوب:**
            حلل التسجيل وأعد النتائج بصيغة JSON التالية بالضبط:

            ```json
            {
                "isGenuineRecording": true/false,
                "genuinenessConfidence": 0.0-1.0,
                "tamperDetection": {
                    "screenRecordingDetected": true/false,
                    "screenRecordingConfidence": 0.0-1.0,
                    "editingDetected": true/false,
                    "editingConfidence": 0.0-1.0,
                    "deepfakeIndicators": true/false,
                    "deepfakeConfidence": 0.0-1.0,
                    "metadataInconsistencies": true/false,
                    "environmentNotes": "ملاحظات عن بيئة التسجيل",
                    "overallTamperConfidence": 0.0-1.0
                },
                "identityVerification": {
                    "faceDetected": true/false,
                    "faceCount": 0,
                    "identityMatch": true/false,
                    "matchConfidence": 0.0-1.0,
                    "qualityAssessment": "تقييم جودة صورة الوجه",
                    "sufficientQuality": true/false
                },
                "flags": [
                    {
                        "code": "FLAG_CODE",
                        "description": "وصف المشكلة بالعربية",
                        "severity": "Low/Medium/High/Critical",
                        "confidence": 0.0-1.0
                    }
                ],
                "summary": "ملخص شامل للتحليل بالعربية",
                "overallConfidence": 0.0-1.0
            }
            ```

            أعد فقط كائن JSON بدون أي نص إضافي.
            """;
    }
}
