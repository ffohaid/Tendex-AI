using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.API.Endpoints.AI;

/// <summary>
/// Minimal API endpoint for general-purpose AI text assistance.
/// Provides text generation, improvement, expansion, summarization, and formalization.
/// </summary>
public static class AiTextAssistEndpoints
{
    public static void MapAiTextAssistEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ai/text")
            .WithTags("AI Text Assistant")
            .RequireAuthorization();

        group.MapPost("/assist", async (
            AiTextAssistRequest request,
            IAiGateway gateway,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
            var tenantId = Guid.TryParse(tenantClaim, out var tid)
                ? tid
                : Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var systemPrompt = BuildSystemPrompt(request);
            var userPrompt = BuildUserPrompt(request);

            var aiRequest = new AiCompletionRequest
            {
                TenantId = tenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                MaxTokensOverride = 4000,
                TemperatureOverride = 0.4
            };

            var response = await gateway.GenerateCompletionAsync(aiRequest, ct);

            if (response.IsSuccess)
            {
                return Results.Ok(new
                {
                    IsSuccess = true,
                    GeneratedText = CleanResponse(response.Content ?? ""),
                    Provider = response.Provider,
                    Model = response.ModelName
                });
            }

            return Results.Ok(new
            {
                IsSuccess = false,
                GeneratedText = "",
                ErrorMessage = response.ErrorMessage ?? "فشل في توليد النص"
            });
        })
        .WithName("AiTextAssist")
        .WithSummary("General-purpose AI text generation and improvement")
        .Produces(StatusCodes.Status200OK);
    }

    private static string BuildSystemPrompt(AiTextAssistRequest request)
    {
        return """
            أنت مساعد ذكاء اصطناعي متخصص في كتابة محتوى كراسات الشروط والمواصفات الحكومية السعودية.
            
            قواعد صارمة:
            1. اكتب باللغة العربية الفصحى الرسمية فقط
            2. استخدم المصطلحات الحكومية والقانونية المعتمدة في المملكة العربية السعودية
            3. التزم بأسلوب الكتابة الرسمية للمنافسات والمشتريات الحكومية
            4. لا تستخدم أي كلمات أو مصطلحات باللغة الإنجليزية
            5. كن دقيقاً ومهنياً وشاملاً
            6. أعد النص العادي فقط بدون أي وسوم HTML أو Markdown
            7. استخدم الأرقام الإنجليزية (1, 2, 3) وليس العربية
            
            السياق:
            - اسم الحقل: """ + (request.FieldName ?? "") + """
            - الغرض من الحقل: """ + (request.FieldPurpose ?? "") + """
            - اسم المشروع: """ + (request.ProjectName ?? "") + """
            - نوع المنافسة: """ + (request.CompetitionType ?? "") + """
            """;
    }

    private static string BuildUserPrompt(AiTextAssistRequest request)
    {
        return request.Action?.ToLowerInvariant() switch
        {
            "generate" => $"""
                قم بكتابة محتوى احترافي ومفصل لحقل "{request.FieldName}" في كراسة الشروط والمواصفات.
                اسم المشروع: {request.ProjectName}
                وصف المشروع: {request.ProjectDescription}
                {(string.IsNullOrEmpty(request.AdditionalContext) ? "" : $"سياق إضافي: {request.AdditionalContext}")}
                {(string.IsNullOrEmpty(request.CustomPrompt) ? "" : $"تعليمات إضافية: {request.CustomPrompt}")}
                """,

            "improve" => $"""
                قم بتحسين النص التالي مع الحفاظ على المعنى الأساسي وتعزيز الجودة والمهنية:
                
                النص الحالي:
                {request.CurrentText}
                
                {(string.IsNullOrEmpty(request.CustomPrompt) ? "" : $"تعليمات إضافية: {request.CustomPrompt}")}
                """,

            "expand" => $"""
                قم بتوسيع النص التالي وإضافة تفاصيل أكثر مع الحفاظ على السياق والمعنى:
                
                النص الحالي:
                {request.CurrentText}
                
                {(string.IsNullOrEmpty(request.CustomPrompt) ? "" : $"تعليمات إضافية: {request.CustomPrompt}")}
                """,

            "summarize" => $"""
                قم بتلخيص النص التالي مع الحفاظ على النقاط الرئيسية والمعلومات المهمة:
                
                النص الحالي:
                {request.CurrentText}
                """,

            "formalize" => $"""
                قم بإعادة صياغة النص التالي بلغة رسمية حكومية سعودية مع الحفاظ على المعنى:
                
                النص الحالي:
                {request.CurrentText}
                """,

            "custom" => $"""
                {request.CustomPrompt}
                
                {(string.IsNullOrEmpty(request.CurrentText) ? "" : $"النص الحالي:\n{request.CurrentText}")}
                
                اسم المشروع: {request.ProjectName}
                اسم الحقل: {request.FieldName}
                """,

            _ => $"""
                قم بكتابة محتوى مناسب لحقل "{request.FieldName}".
                {(string.IsNullOrEmpty(request.CurrentText) ? "" : $"النص الحالي: {request.CurrentText}")}
                {(string.IsNullOrEmpty(request.CustomPrompt) ? "" : $"تعليمات: {request.CustomPrompt}")}
                """
        };
    }

    /// <summary>
    /// Clean the AI response to remove any markdown or HTML formatting
    /// </summary>
    private static string CleanResponse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // Remove markdown bold/italic
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*{1,3}([^*]+)\*{1,3}", "$1");
        // Remove markdown headers
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^#{1,6}\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        // Remove HTML tags
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", "");

        return text.Trim();
    }
}

public sealed record AiTextAssistRequest
{
    public string Action { get; init; } = "generate";
    public string? CurrentText { get; init; }
    public string? FieldName { get; init; }
    public string? FieldPurpose { get; init; }
    public string? ProjectName { get; init; }
    public string? ProjectDescription { get; init; }
    public string? CompetitionType { get; init; }
    public string? AdditionalContext { get; init; }
    public string? CustomPrompt { get; init; }
    public string Language { get; init; } = "ar";
}
