namespace TendexAI.API.Endpoints.Inquiries;

/// <summary>
/// Minimal API endpoints for Inquiries on specification booklets.
/// TASK-904: Provides CRUD operations for inquiries.
/// Returns empty/default data until full backend implementation is complete.
/// </summary>
public static class InquiryEndpoints
{
    public static IEndpointRouteBuilder MapInquiryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/inquiries")
            .WithTags("Inquiries")
            .RequireAuthorization();

        group.MapGet("/", GetInquiriesAsync)
            .WithName("GetInquiries")
            .WithSummary("Retrieve paginated list of inquiries with optional filters");

        group.MapGet("/stats", GetInquiryStatsAsync)
            .WithName("GetInquiryStats")
            .WithSummary("Retrieve inquiry statistics summary");

        group.MapGet("/{id:guid}", GetInquiryByIdAsync)
            .WithName("GetInquiryById")
            .WithSummary("Retrieve a single inquiry by ID");

        group.MapPost("/", CreateInquiryAsync)
            .WithName("CreateInquiry")
            .WithSummary("Create a new inquiry on a specification booklet");

        group.MapPut("/{id:guid}/answer", AnswerInquiryAsync)
            .WithName("AnswerInquiry")
            .WithSummary("Submit an answer for an inquiry");

        return app;
    }

    /// <summary>
    /// Returns paginated inquiries. Currently returns empty list.
    /// </summary>
    private static Task<IResult> GetInquiriesAsync(
        int page = 1,
        int pageSize = 10,
        string? status = null,
        string? priority = null,
        string? competitionId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = new InquiryPagedResultDto
        {
            Items = new List<InquiryDto>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize,
            TotalPages = 0
        };

        return Task.FromResult(Results.Ok(result));
    }

    /// <summary>
    /// Returns inquiry statistics. Currently returns zeroed stats.
    /// </summary>
    private static Task<IResult> GetInquiryStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new InquiryStatsDto
        {
            Total = 0,
            Pending = 0,
            Answered = 0,
            Overdue = 0,
            AverageResponseTimeHours = 0
        };

        return Task.FromResult(Results.Ok(result));
    }

    /// <summary>
    /// Returns a single inquiry by ID. Currently returns 404.
    /// </summary>
    private static Task<IResult> GetInquiryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Results.NotFound(new { Message = "Inquiry not found" }));
    }

    /// <summary>
    /// Creates a new inquiry. Currently returns 201 with placeholder.
    /// </summary>
    private static Task<IResult> CreateInquiryAsync(
        CreateInquiryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Results.Created($"/api/v1/inquiries/{Guid.NewGuid()}", new { Message = "Inquiry creation not yet implemented" }));
    }

    /// <summary>
    /// Answers an inquiry. Currently returns 200 with placeholder.
    /// </summary>
    private static Task<IResult> AnswerInquiryAsync(
        Guid id,
        AnswerInquiryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Results.Ok(new { Message = "Answer submission not yet implemented" }));
    }
}

// ═══════════════════════════════════════════════════════════════
//  DTOs
// ═══════════════════════════════════════════════════════════════

public sealed record InquiryDto
{
    public Guid Id { get; init; }
    public string ReferenceNumber { get; init; } = null!;
    public Guid CompetitionId { get; init; }
    public string CompetitionTitleAr { get; init; } = null!;
    public string CompetitionTitleEn { get; init; } = null!;
    public string CompetitionReferenceNumber { get; init; } = null!;
    public string SubjectAr { get; init; } = null!;
    public string SubjectEn { get; init; } = null!;
    public string BodyAr { get; init; } = null!;
    public string BodyEn { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string Priority { get; init; } = null!;
    public string SubmittedByNameAr { get; init; } = null!;
    public string SubmittedByNameEn { get; init; } = null!;
    public DateTime SubmittedAt { get; init; }
    public DateTime? AnsweredAt { get; init; }
    public string? AnswerAr { get; init; }
    public string? AnswerEn { get; init; }
    public string? AnsweredByNameAr { get; init; }
    public string? AnsweredByNameEn { get; init; }
    public DateTime? SlaDeadline { get; init; }
    public bool IsOverdue { get; init; }
}

public sealed record InquiryPagedResultDto
{
    public List<InquiryDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public sealed record InquiryStatsDto
{
    public int Total { get; init; }
    public int Pending { get; init; }
    public int Answered { get; init; }
    public int Overdue { get; init; }
    public double AverageResponseTimeHours { get; init; }
}

public sealed record CreateInquiryRequestDto
{
    public Guid CompetitionId { get; init; }
    public string SubjectAr { get; init; } = null!;
    public string SubjectEn { get; init; } = null!;
    public string BodyAr { get; init; } = null!;
    public string BodyEn { get; init; } = null!;
    public string Priority { get; init; } = null!;
}

public sealed record AnswerInquiryRequestDto
{
    public string AnswerAr { get; init; } = null!;
    public string AnswerEn { get; init; } = null!;
}
