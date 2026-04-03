using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.API.Endpoints.AI;

/// <summary>
/// AI-powered endpoints for user management assistance.
/// Provides intelligent role suggestions, permission analysis, and user management guidance.
/// </summary>
public static class AiUserManagementEndpoints
{
    public static void MapAiUserManagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ai/user-management")
            .WithTags("AI User Management Assistant")
            .RequireAuthorization();

        group.MapPost("/suggest-role", async (
            AiRoleSuggestionRequest request,
            IAiGateway gateway,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
            var tenantId = Guid.TryParse(tenantClaim, out var tid)
                ? tid
                : Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var systemPrompt = string.Join("\n", new[]
            {
                "أنت مساعد ذكاء اصطناعي متخصص في إدارة الأدوار والصلاحيات في منصة المنافسات والمشتريات الحكومية السعودية.",
                "",
                "قواعد صارمة:",
                "1. اكتب بالعربية الفصحى الرسمية فقط",
                "2. استخدم المصطلحات الحكومية المعتمدة في المملكة العربية السعودية",
                "3. استخدم الأرقام الإنجليزية (1, 2, 3)",
                "4. أعد الإجابة بتنسيق JSON فقط بدون أي نص إضافي",
                "5. كن دقيقاً ومهنياً",
                "",
                "المراحل المتاحة في النظام:",
                "1. إعداد الكراسة",
                "2. اعتماد الكراسة",
                "3. طرح الكراسة",
                "4. استقبال العروض",
                "5. التحليل الفني",
                "6. التحليل المالي",
                "7. إشعار الترسية",
                "8. إجازة العقد",
                "9. توقيع العقد",
                "",
                "الأدوار النظامية المتاحة:",
                "- مدير النظام (SystemAdmin): صلاحيات كاملة",
                "- مشغل النظام (Operator): إدارة الإعدادات والمستخدمين",
                "- مدير المنافسات (CompetitionManager): إدارة المنافسات",
                "- عضو لجنة (CommitteeMember): المشاركة في اللجان",
                "- مراجع (Reviewer): مراجعة العروض",
                "- مراقب (Auditor): مراقبة العمليات",
                "- مورد (Supplier): تقديم العروض"
            });

            var userPrompt = string.Join("\n", new[]
            {
                "بناءً على المعلومات التالية، اقترح الدور المناسب والصلاحيات المطلوبة:",
                "",
                $"المسمى الوظيفي: {request.JobTitle ?? "غير محدد"}",
                $"القسم: {request.Department ?? "غير محدد"}",
                $"المسؤوليات: {request.Responsibilities ?? "غير محددة"}",
                $"الأدوار الحالية المسندة: {request.CurrentRoles ?? "لا يوجد"}",
                "",
                "أعد الإجابة بتنسيق JSON التالي فقط:",
                "{",
                "  \"suggestedRoleNameAr\": \"اسم الدور بالعربية\",",
                "  \"suggestedRoleNameEn\": \"Role Name in English\",",
                "  \"reason\": \"سبب الاقتراح\",",
                "  \"suggestedPermissions\": [\"صلاحية 1\", \"صلاحية 2\"],",
                "  \"suggestedPhases\": [\"المرحلة 1\", \"المرحلة 2\"],",
                "  \"riskNotes\": \"ملاحظات أمنية إن وجدت\"",
                "}"
            });

            var aiRequest = new AiCompletionRequest
            {
                TenantId = tenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                MaxTokensOverride = 2000,
                TemperatureOverride = 0.3
            };

            var response = await gateway.GenerateCompletionAsync(aiRequest, ct);

            if (response.IsSuccess)
            {
                return Results.Ok(new
                {
                    IsSuccess = true,
                    Suggestion = response.Content,
                    Provider = response.Provider.ToString(),
                    Model = response.ModelName
                });
            }

            return Results.Ok(new
            {
                IsSuccess = false,
                Suggestion = "",
                ErrorMessage = response.ErrorMessage ?? "فشل في توليد الاقتراح"
            });
        })
        .WithName("AiSuggestRole")
        .WithSummary("AI-powered role suggestion based on job title and responsibilities")
        .Produces(StatusCodes.Status200OK);

        group.MapPost("/analyze-permissions", async (
            AiPermissionAnalysisRequest request,
            IAiGateway gateway,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
            var tenantId = Guid.TryParse(tenantClaim, out var tid)
                ? tid
                : Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var systemPrompt = string.Join("\n", new[]
            {
                "أنت مساعد ذكاء اصطناعي متخصص في تحليل الصلاحيات والأمان في منصة المنافسات والمشتريات الحكومية السعودية.",
                "",
                "قواعد صارمة:",
                "1. اكتب بالعربية الفصحى الرسمية فقط",
                "2. استخدم الأرقام الإنجليزية (1, 2, 3)",
                "3. أعد الإجابة بتنسيق JSON فقط بدون أي نص إضافي",
                "4. كن دقيقاً في تحليل المخاطر الأمنية",
                "5. اتبع مبدأ الحد الأدنى من الصلاحيات (Least Privilege)"
            });

            var userPrompt = string.Join("\n", new[]
            {
                "حلل الصلاحيات التالية وقدم توصيات:",
                "",
                $"اسم الدور: {request.RoleName ?? "غير محدد"}",
                $"الصلاحيات الحالية: {request.CurrentPermissions ?? "لا يوجد"}",
                $"عدد المستخدمين المسندين: {request.UserCount}",
                $"نوع التحليل المطلوب: {request.AnalysisType ?? "شامل"}",
                "",
                "أعد الإجابة بتنسيق JSON التالي فقط:",
                "{",
                "  \"summary\": \"ملخص التحليل\",",
                "  \"riskLevel\": \"منخفض/متوسط/عالي\",",
                "  \"recommendations\": [\"توصية 1\", \"توصية 2\"],",
                "  \"excessivePermissions\": [\"صلاحية زائدة 1\"],",
                "  \"missingPermissions\": [\"صلاحية مفقودة 1\"],",
                "  \"securityNotes\": \"ملاحظات أمنية\"",
                "}"
            });

            var aiRequest = new AiCompletionRequest
            {
                TenantId = tenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                MaxTokensOverride = 2000,
                TemperatureOverride = 0.3
            };

            var response = await gateway.GenerateCompletionAsync(aiRequest, ct);

            if (response.IsSuccess)
            {
                return Results.Ok(new
                {
                    IsSuccess = true,
                    Analysis = response.Content,
                    Provider = response.Provider.ToString(),
                    Model = response.ModelName
                });
            }

            return Results.Ok(new
            {
                IsSuccess = false,
                Analysis = "",
                ErrorMessage = response.ErrorMessage ?? "فشل في تحليل الصلاحيات"
            });
        })
        .WithName("AiAnalyzePermissions")
        .WithSummary("AI-powered permission analysis and security recommendations")
        .Produces(StatusCodes.Status200OK);

        group.MapPost("/assist", async (
            AiUserManagementAssistRequest request,
            IAiGateway gateway,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
            var tenantId = Guid.TryParse(tenantClaim, out var tid)
                ? tid
                : Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            var systemPrompt = string.Join("\n", new[]
            {
                "أنت مساعد ذكاء اصطناعي متخصص في إدارة المستخدمين والأدوار والصلاحيات في منصة المنافسات والمشتريات الحكومية السعودية.",
                "",
                "قواعد صارمة:",
                "1. اكتب بالعربية الفصحى الرسمية فقط",
                "2. لا تستخدم أي كلمات أو مصطلحات بالإنجليزية",
                "3. استخدم الأرقام الإنجليزية (1, 2, 3)",
                "4. كن دقيقاً ومهنياً وشاملاً",
                "5. قدم إجابات عملية وقابلة للتطبيق",
                "",
                "مجالات خبرتك:",
                "- إدارة المستخدمين وتعيين الأدوار",
                "- تصميم هيكل الصلاحيات",
                "- أفضل ممارسات الأمان",
                "- نظام المنافسات والمشتريات الحكومية السعودي",
                "- اللجان ومراحل المنافسات",
                "- مسارات الاعتماد وسير العمل"
            });

            var aiRequest = new AiCompletionRequest
            {
                TenantId = tenantId,
                SystemPrompt = systemPrompt,
                UserPrompt = request.Question,
                MaxTokensOverride = 3000,
                TemperatureOverride = 0.4
            };

            var response = await gateway.GenerateCompletionAsync(aiRequest, ct);

            if (response.IsSuccess)
            {
                return Results.Ok(new
                {
                    IsSuccess = true,
                    Answer = response.Content,
                    Provider = response.Provider.ToString(),
                    Model = response.ModelName
                });
            }

            return Results.Ok(new
            {
                IsSuccess = false,
                Answer = "",
                ErrorMessage = response.ErrorMessage ?? "فشل في الإجابة على السؤال"
            });
        })
        .WithName("AiUserManagementAssist")
        .WithSummary("General AI assistant for user management questions")
        .Produces(StatusCodes.Status200OK);
    }
}

public sealed record AiRoleSuggestionRequest
{
    public string? JobTitle { get; init; }
    public string? Department { get; init; }
    public string? Responsibilities { get; init; }
    public string? CurrentRoles { get; init; }
}

public sealed record AiPermissionAnalysisRequest
{
    public string? RoleName { get; init; }
    public string? CurrentPermissions { get; init; }
    public int UserCount { get; init; }
    public string? AnalysisType { get; init; }
}

public sealed record AiUserManagementAssistRequest
{
    public required string Question { get; init; }
}
