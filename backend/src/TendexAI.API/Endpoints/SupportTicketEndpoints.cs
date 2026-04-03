using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.API.Endpoints;

/// <summary>
/// Defines Minimal API endpoints for the Support Ticket system.
/// Handles ticket creation, messaging, status updates, and AI assistance.
/// </summary>
public static class SupportTicketEndpoints
{
    public static IEndpointRouteBuilder MapSupportTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/support-tickets")
            .WithTags("Support Tickets")
            .RequireAuthorization();

        // ----- Ticket CRUD -----
        group.MapGet("/", GetTicketsAsync)
            .WithName("GetSupportTickets")
            .WithSummary("Get paginated list of support tickets");

        group.MapGet("/{id:guid}", GetTicketByIdAsync)
            .WithName("GetSupportTicketById")
            .WithSummary("Get a support ticket with messages");

        group.MapPost("/", CreateTicketAsync)
            .WithName("CreateSupportTicket")
            .WithSummary("Create a new support ticket");

        group.MapPut("/{id:guid}/status", UpdateTicketStatusAsync)
            .WithName("UpdateSupportTicketStatus")
            .WithSummary("Update ticket status");

        group.MapPut("/{id:guid}/assign", AssignTicketAsync)
            .WithName("AssignSupportTicket")
            .WithSummary("Assign ticket to an operator");

        group.MapPut("/{id:guid}/resolve", ResolveTicketAsync)
            .WithName("ResolveSupportTicket")
            .WithSummary("Resolve a support ticket");

        group.MapPut("/{id:guid}/rate", RateTicketAsync)
            .WithName("RateSupportTicket")
            .WithSummary("Rate a resolved ticket");

        // ----- Messages -----
        group.MapPost("/{id:guid}/messages", AddMessageAsync)
            .WithName("AddSupportTicketMessage")
            .WithSummary("Add a message to a support ticket");

        group.MapPut("/{id:guid}/messages/read", MarkMessagesReadAsync)
            .WithName("MarkSupportTicketMessagesRead")
            .WithSummary("Mark all messages as read");

        // ----- AI Features -----
        group.MapPost("/{id:guid}/ai-analyze", AiAnalyzeTicketAsync)
            .WithName("AiAnalyzeSupportTicket")
            .WithSummary("Use AI to analyze and suggest resolution");

        group.MapPost("/{id:guid}/ai-reply", AiGenerateReplyAsync)
            .WithName("AiGenerateSupportReply")
            .WithSummary("Use AI to generate a reply suggestion");

        // ----- Stats -----
        group.MapGet("/stats", GetTicketStatsAsync)
            .WithName("GetSupportTicketStats")
            .WithSummary("Get ticket statistics and counts");

        return app;
    }

    // ==================== Handlers ====================

    private static async Task<IResult> GetTicketsAsync(
        [FromServices] ISupportTicketRepository repo,
        [FromServices] ICurrentUserService currentUser,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] SupportTicketStatus? status = null,
        [FromQuery] SupportTicketCategory? category = null,
        [FromQuery] SupportTicketPriority? priority = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        // If not operator, restrict to own tenant
        var effectiveTenantId = tenantId;
        if (currentUser.TenantId.HasValue && !IsOperator(currentUser))
        {
            effectiveTenantId = currentUser.TenantId;
        }

        var (items, totalCount) = await repo.GetPagedAsync(
            page, pageSize, effectiveTenantId, status, category, priority, search, ct);

        var dtos = items.Select(t => MapToDto(t)).ToList();

        return Results.Ok(new
        {
            items = dtos,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    private static async Task<IResult> GetTicketByIdAsync(
        Guid id,
        [FromServices] ISupportTicketRepository repo,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var ticket = await repo.GetByIdWithMessagesAsync(id, ct);
        if (ticket == null)
            return Results.NotFound(new { message = "Ticket not found" });

        // Security: tenant users can only see their own tickets
        if (!IsOperator(currentUser) && currentUser.TenantId.HasValue && ticket.TenantId != currentUser.TenantId)
            return Results.Forbid();

        return Results.Ok(MapToDetailDto(ticket));
    }

    private static async Task<IResult> CreateTicketAsync(
        [FromBody] CreateTicketRequest request,
        [FromServices] ISupportTicketRepository repo,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var ticketNumber = await repo.GetNextTicketNumberAsync(ct);
        var year = DateTime.UtcNow.Year;

        var ticket = new SupportTicket
        {
            TenantId = currentUser.TenantId ?? Guid.Empty,
            TicketNumber = $"TKT-{year}-{ticketNumber:D4}",
            Subject = request.Subject,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            CreatedByUserId = currentUser.UserId ?? Guid.Empty,
            CreatedByUserName = currentUser.UserName ?? "Unknown",
            CreatedByUserEmail = currentUser.Email ?? "unknown@unknown.com",
            Status = SupportTicketStatus.Open
        };

        await repo.CreateAsync(ticket, ct);

        // Add the initial description as the first message
        var initialMessage = new SupportTicketMessage
        {
            SupportTicketId = ticket.Id,
            SenderUserId = ticket.CreatedByUserId,
            SenderName = ticket.CreatedByUserName,
            SenderEmail = ticket.CreatedByUserEmail,
            IsOperatorMessage = false,
            Content = request.Description,
            IsAiGenerated = false
        };
        await repo.AddMessageAsync(initialMessage, ct);

        return Results.Created($"/api/v1/support-tickets/{ticket.Id}", MapToDto(ticket));
    }

    private static async Task<IResult> UpdateTicketStatusAsync(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        [FromServices] ISupportTicketRepository repo,
        CancellationToken ct = default)
    {
        var ticket = await repo.GetByIdAsync(id, ct);
        if (ticket == null)
            return Results.NotFound(new { message = "Ticket not found" });

        ticket.Status = request.Status;
        if (request.Status == SupportTicketStatus.Resolved)
            ticket.ResolvedAt = DateTime.UtcNow;
        if (request.Status == SupportTicketStatus.Closed)
            ticket.ClosedAt = DateTime.UtcNow;

        await repo.UpdateAsync(ticket, ct);
        return Results.Ok(MapToDto(ticket));
    }

    private static async Task<IResult> AssignTicketAsync(
        Guid id,
        [FromBody] AssignTicketRequest request,
        [FromServices] ISupportTicketRepository repo,
        CancellationToken ct = default)
    {
        var ticket = await repo.GetByIdAsync(id, ct);
        if (ticket == null)
            return Results.NotFound(new { message = "Ticket not found" });

        ticket.AssignedToUserId = request.UserId;
        ticket.AssignedToUserName = request.UserName;
        if (ticket.Status == SupportTicketStatus.Open)
            ticket.Status = SupportTicketStatus.InProgress;

        await repo.UpdateAsync(ticket, ct);
        return Results.Ok(MapToDto(ticket));
    }

    private static async Task<IResult> ResolveTicketAsync(
        Guid id,
        [FromBody] ResolveTicketRequest request,
        [FromServices] ISupportTicketRepository repo,
        CancellationToken ct = default)
    {
        var ticket = await repo.GetByIdAsync(id, ct);
        if (ticket == null)
            return Results.NotFound(new { message = "Ticket not found" });

        ticket.Status = SupportTicketStatus.Resolved;
        ticket.ResolutionNotes = request.ResolutionNotes;
        ticket.ResolvedAt = DateTime.UtcNow;

        await repo.UpdateAsync(ticket, ct);
        return Results.Ok(MapToDto(ticket));
    }

    private static async Task<IResult> RateTicketAsync(
        Guid id,
        [FromBody] RateTicketRequest request,
        [FromServices] ISupportTicketRepository repo,
        CancellationToken ct = default)
    {
        var ticket = await repo.GetByIdAsync(id, ct);
        if (ticket == null)
            return Results.NotFound(new { message = "Ticket not found" });

        ticket.SatisfactionRating = request.Rating;
        ticket.SatisfactionFeedback = request.Feedback;
        ticket.Status = SupportTicketStatus.Closed;
        ticket.ClosedAt = DateTime.UtcNow;

        await repo.UpdateAsync(ticket, ct);
        return Results.Ok(MapToDto(ticket));
    }

    private static async Task<IResult> AddMessageAsync(
        Guid id,
        [FromBody] AddMessageRequest request,
        [FromServices] ISupportTicketRepository repo,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var ticket = await repo.GetByIdAsync(id, ct);
        if (ticket == null)
            return Results.NotFound(new { message = "Ticket not found" });

        var isOperator = IsOperator(currentUser);

        var message = new SupportTicketMessage
        {
            SupportTicketId = id,
            SenderUserId = currentUser.UserId ?? Guid.Empty,
            SenderName = currentUser.UserName ?? "Unknown",
            SenderEmail = currentUser.Email ?? "unknown@unknown.com",
            IsOperatorMessage = isOperator,
            Content = request.Content,
            AttachmentUrl = request.AttachmentUrl,
            AttachmentName = request.AttachmentName,
            IsAiGenerated = false
        };

        await repo.AddMessageAsync(message, ct);

        // Update ticket status based on who replied
        if (isOperator)
        {
            ticket.Status = SupportTicketStatus.WaitingForCustomer;
            if (ticket.FirstResponseAt == null)
                ticket.FirstResponseAt = DateTime.UtcNow;
        }
        else
        {
            ticket.Status = SupportTicketStatus.WaitingForOperator;
        }

        await repo.UpdateAsync(ticket, ct);

        return Results.Ok(new
        {
            id = message.Id,
            senderName = message.SenderName,
            isOperatorMessage = message.IsOperatorMessage,
            content = message.Content,
            attachmentUrl = message.AttachmentUrl,
            attachmentName = message.AttachmentName,
            isAiGenerated = message.IsAiGenerated,
            createdAt = message.CreatedAt
        });
    }

    private static async Task<IResult> MarkMessagesReadAsync(
        Guid id,
        [FromServices] ISupportTicketRepository repo,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var ticket = await repo.GetByIdWithMessagesAsync(id, ct);
        if (ticket == null)
            return Results.NotFound(new { message = "Ticket not found" });

        var isOperator = IsOperator(currentUser);
        foreach (var msg in ticket.Messages.Where(m => !m.IsRead && m.IsOperatorMessage != isOperator))
        {
            msg.IsRead = true;
        }

        await repo.UpdateAsync(ticket, ct);
        return Results.Ok(new { message = "Messages marked as read" });
    }

    private static async Task<IResult> AiAnalyzeTicketAsync(
        Guid id,
        [FromServices] ISupportTicketRepository repo,
        CancellationToken ct = default)
    {
        var ticket = await repo.GetByIdWithMessagesAsync(id, ct);
        if (ticket == null)
            return Results.NotFound(new { message = "Ticket not found" });

        // AI Analysis - generate summary, sentiment, suggested category and priority
        var allMessages = string.Join("\n", ticket.Messages.Select(m => $"{m.SenderName}: {m.Content}"));
        var description = ticket.Description;

        // Simple AI analysis using pattern matching (can be enhanced with actual AI service)
        ticket.AiSummary = GenerateAiSummary(ticket);
        ticket.AiSentiment = DetectSentiment(description);
        ticket.AiSuggestedCategory = SuggestCategory(description);
        ticket.AiSuggestedPriority = SuggestPriority(description, ticket.AiSentiment);
        ticket.AiSuggestedResolution = GenerateResolutionSuggestion(ticket);

        await repo.UpdateAsync(ticket, ct);

        return Results.Ok(new
        {
            summary = ticket.AiSummary,
            sentiment = ticket.AiSentiment,
            suggestedCategory = ticket.AiSuggestedCategory?.ToString(),
            suggestedPriority = ticket.AiSuggestedPriority?.ToString(),
            suggestedResolution = ticket.AiSuggestedResolution
        });
    }

    private static async Task<IResult> AiGenerateReplyAsync(
        Guid id,
        [FromBody] AiReplyRequest request,
        [FromServices] ISupportTicketRepository repo,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var ticket = await repo.GetByIdWithMessagesAsync(id, ct);
        if (ticket == null)
            return Results.NotFound(new { message = "Ticket not found" });

        var aiReply = GenerateAiReplyText(ticket, request.Tone ?? "professional");

        if (request.AutoSend)
        {
            var message = new SupportTicketMessage
            {
                SupportTicketId = id,
                SenderUserId = currentUser.UserId ?? Guid.Empty,
                SenderName = "المساعد الذكي",
                SenderEmail = "ai@tendex.ai",
                IsOperatorMessage = true,
                Content = aiReply,
                IsAiGenerated = true
            };

            await repo.AddMessageAsync(message, ct);

            ticket.Status = SupportTicketStatus.WaitingForCustomer;
            if (ticket.FirstResponseAt == null)
                ticket.FirstResponseAt = DateTime.UtcNow;
            await repo.UpdateAsync(ticket, ct);
        }

        return Results.Ok(new
        {
            suggestedReply = aiReply,
            autoSent = request.AutoSend
        });
    }

    private static async Task<IResult> GetTicketStatsAsync(
        [FromServices] ISupportTicketRepository repo,
        [FromServices] ICurrentUserService currentUser,
        [FromQuery] Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var effectiveTenantId = tenantId;
        if (!IsOperator(currentUser) && currentUser.TenantId.HasValue)
            effectiveTenantId = currentUser.TenantId;

        var statusCounts = await repo.GetStatusCountsAsync(effectiveTenantId, ct);
        var unreadCount = await repo.GetUnreadCountAsync(effectiveTenantId, IsOperator(currentUser), ct);

        return Results.Ok(new
        {
            statusCounts = statusCounts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            totalOpen = statusCounts.GetValueOrDefault(SupportTicketStatus.Open, 0),
            totalInProgress = statusCounts.GetValueOrDefault(SupportTicketStatus.InProgress, 0),
            totalResolved = statusCounts.GetValueOrDefault(SupportTicketStatus.Resolved, 0),
            totalClosed = statusCounts.GetValueOrDefault(SupportTicketStatus.Closed, 0),
            total = statusCounts.Values.Sum(),
            unreadMessages = unreadCount
        });
    }

    // ==================== Helper Methods ====================

    private static bool IsOperator(ICurrentUserService currentUser)
    {
        return currentUser.Roles?.Any(r =>
            r.Contains("Operator", StringComparison.OrdinalIgnoreCase) ||
            r.Contains("Super Admin", StringComparison.OrdinalIgnoreCase)) ?? false;
    }

    private static object MapToDto(SupportTicket t) => new
    {
        id = t.Id,
        ticketNumber = t.TicketNumber,
        subject = t.Subject,
        description = t.Description,
        category = t.Category.ToString(),
        priority = t.Priority.ToString(),
        status = t.Status.ToString(),
        createdByUserName = t.CreatedByUserName,
        createdByUserEmail = t.CreatedByUserEmail,
        assignedToUserName = t.AssignedToUserName,
        tenantName = t.Tenant?.NameAr ?? t.Tenant?.NameEn ?? "",
        tenantId = t.TenantId,
        aiSummary = t.AiSummary,
        aiSentiment = t.AiSentiment,
        createdAt = t.CreatedAt,
        updatedAt = t.UpdatedAt,
        resolvedAt = t.ResolvedAt,
        closedAt = t.ClosedAt,
        firstResponseAt = t.FirstResponseAt,
        satisfactionRating = t.SatisfactionRating
    };

    private static object MapToDetailDto(SupportTicket t) => new
    {
        id = t.Id,
        ticketNumber = t.TicketNumber,
        subject = t.Subject,
        description = t.Description,
        category = t.Category.ToString(),
        priority = t.Priority.ToString(),
        status = t.Status.ToString(),
        createdByUserId = t.CreatedByUserId,
        createdByUserName = t.CreatedByUserName,
        createdByUserEmail = t.CreatedByUserEmail,
        assignedToUserId = t.AssignedToUserId,
        assignedToUserName = t.AssignedToUserName,
        tenantName = t.Tenant?.NameAr ?? t.Tenant?.NameEn ?? "",
        tenantId = t.TenantId,
        aiSummary = t.AiSummary,
        aiSuggestedResolution = t.AiSuggestedResolution,
        aiSentiment = t.AiSentiment,
        aiSuggestedCategory = t.AiSuggestedCategory?.ToString(),
        aiSuggestedPriority = t.AiSuggestedPriority?.ToString(),
        resolutionNotes = t.ResolutionNotes,
        satisfactionRating = t.SatisfactionRating,
        satisfactionFeedback = t.SatisfactionFeedback,
        createdAt = t.CreatedAt,
        updatedAt = t.UpdatedAt,
        resolvedAt = t.ResolvedAt,
        closedAt = t.ClosedAt,
        firstResponseAt = t.FirstResponseAt,
        messages = t.Messages.Select(m => new
        {
            id = m.Id,
            senderUserId = m.SenderUserId,
            senderName = m.SenderName,
            isOperatorMessage = m.IsOperatorMessage,
            isAiGenerated = m.IsAiGenerated,
            content = m.Content,
            attachmentUrl = m.AttachmentUrl,
            attachmentName = m.AttachmentName,
            isRead = m.IsRead,
            createdAt = m.CreatedAt
        }).ToList()
    };

    // ==================== AI Helper Methods ====================

    private static string GenerateAiSummary(SupportTicket ticket)
    {
        var category = ticket.Category switch
        {
            SupportTicketCategory.TechnicalIssue => "مشكلة تقنية",
            SupportTicketCategory.FeatureRequest => "طلب ميزة جديدة",
            SupportTicketCategory.AccountAccess => "مشكلة في الوصول للحساب",
            SupportTicketCategory.BillingSubscription => "استفسار عن الاشتراك",
            SupportTicketCategory.TrainingDocumentation => "طلب تدريب أو توثيق",
            SupportTicketCategory.IntegrationApi => "مشكلة في التكامل",
            SupportTicketCategory.PerformanceIssue => "مشكلة في الأداء",
            SupportTicketCategory.DataReporting => "مشكلة في البيانات أو التقارير",
            SupportTicketCategory.GeneralInquiry => "استفسار عام",
            _ => "أخرى"
        };

        return $"تذكرة دعم فني من نوع '{category}' بعنوان '{ticket.Subject}'. " +
               $"تم إنشاؤها بواسطة {ticket.CreatedByUserName}. " +
               $"الوصف: {(ticket.Description.Length > 200 ? ticket.Description[..200] + "..." : ticket.Description)}";
    }

    private static string DetectSentiment(string text)
    {
        var negativeWords = new[] { "مشكلة", "خطأ", "عطل", "لا يعمل", "توقف", "بطيء", "سيء", "عاجل", "حرج", "خلل" };
        var positiveWords = new[] { "شكر", "ممتاز", "جيد", "رائع", "مساعدة", "استفسار" };

        var lowerText = text.ToLower(System.Globalization.CultureInfo.InvariantCulture);
        var negCount = negativeWords.Count(w => lowerText.Contains(w, StringComparison.OrdinalIgnoreCase));
        var posCount = positiveWords.Count(w => lowerText.Contains(w, StringComparison.OrdinalIgnoreCase));

        if (negCount > posCount + 1) return "سلبي";
        if (posCount > negCount) return "إيجابي";
        return "محايد";
    }

    private static SupportTicketCategory SuggestCategory(string text)
    {
        var lowerText = text.ToLower(System.Globalization.CultureInfo.InvariantCulture);

        if (lowerText.Contains("تسجيل") || lowerText.Contains("دخول") || lowerText.Contains("كلمة مرور") || lowerText.Contains("صلاحية"))
            return SupportTicketCategory.AccountAccess;
        if (lowerText.Contains("بطيء") || lowerText.Contains("أداء") || lowerText.Contains("تحميل"))
            return SupportTicketCategory.PerformanceIssue;
        if (lowerText.Contains("اشتراك") || lowerText.Contains("فاتورة") || lowerText.Contains("دفع"))
            return SupportTicketCategory.BillingSubscription;
        if (lowerText.Contains("تقرير") || lowerText.Contains("بيانات") || lowerText.Contains("إحصائي"))
            return SupportTicketCategory.DataReporting;
        if (lowerText.Contains("تدريب") || lowerText.Contains("دليل") || lowerText.Contains("شرح"))
            return SupportTicketCategory.TrainingDocumentation;
        if (lowerText.Contains("ميزة") || lowerText.Contains("إضافة") || lowerText.Contains("تطوير"))
            return SupportTicketCategory.FeatureRequest;
        if (lowerText.Contains("ربط") || lowerText.Contains("تكامل") || lowerText.Contains("api"))
            return SupportTicketCategory.IntegrationApi;
        if (lowerText.Contains("خطأ") || lowerText.Contains("عطل") || lowerText.Contains("مشكلة") || lowerText.Contains("لا يعمل"))
            return SupportTicketCategory.TechnicalIssue;

        return SupportTicketCategory.GeneralInquiry;
    }

    private static SupportTicketPriority SuggestPriority(string text, string? sentiment)
    {
        var lowerText = text.ToLower(System.Globalization.CultureInfo.InvariantCulture);

        if (lowerText.Contains("عاجل") || lowerText.Contains("حرج") || lowerText.Contains("متوقف") || lowerText.Contains("لا يعمل بالكامل"))
            return SupportTicketPriority.Critical;
        if (lowerText.Contains("مهم") || lowerText.Contains("يؤثر") || sentiment == "سلبي")
            return SupportTicketPriority.High;
        if (lowerText.Contains("استفسار") || lowerText.Contains("سؤال"))
            return SupportTicketPriority.Low;

        return SupportTicketPriority.Medium;
    }

    private static string GenerateResolutionSuggestion(SupportTicket ticket)
    {
        return ticket.Category switch
        {
            SupportTicketCategory.TechnicalIssue =>
                "يُنصح بالتحقق من سجلات النظام وإعادة تشغيل الخدمة المتأثرة. في حال استمرار المشكلة، يرجى جمع معلومات إضافية عن البيئة والخطوات التي أدت للمشكلة.",
            SupportTicketCategory.AccountAccess =>
                "يُنصح بالتحقق من صلاحيات المستخدم وحالة الحساب. يمكن إعادة تعيين كلمة المرور أو التحقق من إعدادات المصادقة الثنائية.",
            SupportTicketCategory.PerformanceIssue =>
                "يُنصح بمراجعة استهلاك الموارد (CPU, RAM, Disk) والتحقق من استعلامات قاعدة البيانات البطيئة. قد يكون من المفيد تحسين الفهارس أو زيادة موارد الخادم.",
            SupportTicketCategory.BillingSubscription =>
                "يُنصح بمراجعة تفاصيل الاشتراك الحالي والتحقق من حالة الدفع. يمكن التواصل مع قسم المالية لتحديث معلومات الفوترة.",
            SupportTicketCategory.TrainingDocumentation =>
                "يُنصح بتوفير دليل المستخدم المحدث وجدولة جلسة تدريبية عبر الإنترنت. يمكن أيضاً مشاركة مقاطع فيديو تعليمية.",
            _ =>
                "يُنصح بتحليل المشكلة بشكل أعمق وجمع معلومات إضافية من المستخدم لتحديد الحل الأنسب."
        };
    }

    private static string GenerateAiReplyText(SupportTicket ticket, string tone)
    {
        var greeting = tone == "formal" ? "حضرة المستخدم الكريم" : "مرحباً";
        var category = ticket.Category switch
        {
            SupportTicketCategory.TechnicalIssue => "مشكلة تقنية",
            SupportTicketCategory.AccountAccess => "مشكلة في الوصول",
            SupportTicketCategory.PerformanceIssue => "مشكلة في الأداء",
            SupportTicketCategory.BillingSubscription => "استفسار عن الاشتراك",
            SupportTicketCategory.TrainingDocumentation => "طلب تدريب",
            SupportTicketCategory.FeatureRequest => "طلب ميزة",
            _ => "استفسار"
        };

        return $"{greeting},\n\n" +
               $"شكراً لتواصلكم معنا بخصوص '{ticket.Subject}'.\n\n" +
               $"لقد تم استلام تذكرتكم رقم {ticket.TicketNumber} وتصنيفها كـ '{category}'.\n\n" +
               $"فريق الدعم الفني يعمل على معالجة طلبكم وسيتم الرد عليكم في أقرب وقت ممكن.\n\n" +
               $"في حال وجود أي معلومات إضافية ترغبون في مشاركتها، يرجى الرد على هذه الرسالة.\n\n" +
               $"مع أطيب التحيات،\nفريق الدعم الفني - منصة نتاق";
    }

    // ==================== Request Models ====================

    public record CreateTicketRequest(
        string Subject,
        string Description,
        SupportTicketCategory Category,
        SupportTicketPriority Priority);

    public record UpdateStatusRequest(SupportTicketStatus Status);

    public record AssignTicketRequest(Guid UserId, string UserName);

    public record ResolveTicketRequest(string ResolutionNotes);

    public record RateTicketRequest(int Rating, string? Feedback);

    public record AddMessageRequest(string Content, string? AttachmentUrl = null, string? AttachmentName = null);

    public record AiReplyRequest(string? Tone = "professional", bool AutoSend = false);
}
