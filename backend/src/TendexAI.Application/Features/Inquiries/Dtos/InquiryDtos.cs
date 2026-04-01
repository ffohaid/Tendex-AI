using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Inquiries.Dtos;

// ═══════════════════════════════════════════════════════════════
//  Response DTOs
// ═══════════════════════════════════════════════════════════════

public sealed record InquiryDto
{
    public Guid Id { get; init; }
    public Guid CompetitionId { get; init; }
    public string? CompetitionName { get; init; }
    public string ReferenceNumber { get; init; } = null!;
    public string QuestionText { get; init; } = null!;
    public string Category { get; init; } = null!;
    public string Priority { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string? SupplierName { get; init; }
    public string? EtimadReferenceNumber { get; init; }
    public string? ApprovedAnswer { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public string? AssignedToUserName { get; init; }
    public Guid? AssignedToCommitteeId { get; init; }
    public DateTime? SlaDeadline { get; init; }
    public bool IsOverdue { get; init; }
    public DateTime? AnsweredAt { get; init; }
    public string? AnsweredBy { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public string? ApprovedBy { get; init; }
    public string? RejectionReason { get; init; }
    public bool IsAiAssisted { get; init; }
    public string? InternalNotes { get; init; }
    public bool IsExportedToEtimad { get; init; }
    public DateTime? ExportedToEtimadAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public List<InquiryResponseDto> Responses { get; init; } = [];
}

public sealed record InquiryResponseDto
{
    public Guid Id { get; init; }
    public string AnswerText { get; init; } = null!;
    public bool IsAiGenerated { get; init; }
    public int? AiConfidenceScore { get; init; }
    public string? AiModelUsed { get; init; }
    public string? AiSources { get; init; }
    public bool IsSelected { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public sealed record InquiryStatisticsDto
{
    public int Total { get; init; }
    public int New { get; init; }
    public int InProgress { get; init; }
    public int PendingApproval { get; init; }
    public int Approved { get; init; }
    public int Rejected { get; init; }
    public int Overdue { get; init; }
    public double AverageResponseTimeHours { get; init; }
}

public sealed record InquiryPagedResultDto
{
    public List<InquiryDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

// ═══════════════════════════════════════════════════════════════
//  Request DTOs
// ═══════════════════════════════════════════════════════════════

public sealed record CreateInquiryRequestDto
{
    public Guid CompetitionId { get; init; }
    public string QuestionText { get; init; } = null!;
    public string Category { get; init; } = "General";
    public string Priority { get; init; } = "Medium";
    public string? SupplierName { get; init; }
    public string? EtimadReferenceNumber { get; init; }
    public string? InternalNotes { get; init; }
}

public sealed record UpdateInquiryRequestDto
{
    public string QuestionText { get; init; } = null!;
    public string Category { get; init; } = null!;
    public string Priority { get; init; } = null!;
    public string? SupplierName { get; init; }
    public string? InternalNotes { get; init; }
}

public sealed record AssignInquiryRequestDto
{
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public Guid? CommitteeId { get; init; }
}

public sealed record SubmitAnswerRequestDto
{
    public string AnswerText { get; init; } = null!;
    public bool IsAiAssisted { get; init; }
}

public sealed record RejectAnswerRequestDto
{
    public string Reason { get; init; } = null!;
}

public sealed record CloseInquiryRequestDto
{
    public string? Reason { get; init; }
}

public sealed record GenerateAiAnswerRequestDto
{
    public string? AdditionalContext { get; init; }
    public bool UseRag { get; init; } = true;
}

public sealed record BulkImportInquiryItemDto
{
    public string QuestionText { get; init; } = null!;
    public string? SupplierName { get; init; }
    public string? EtimadReferenceNumber { get; init; }
    public string Category { get; init; } = "General";
    public string Priority { get; init; } = "Medium";
}

public sealed record BulkImportInquiriesRequestDto
{
    public Guid CompetitionId { get; init; }
    public List<BulkImportInquiryItemDto> Inquiries { get; init; } = [];
}

public sealed record AiAnswerResponseDto
{
    public string AnswerText { get; init; } = null!;
    public int ConfidenceScore { get; init; }
    public string ModelUsed { get; init; } = null!;
    public string? Sources { get; init; }
    public Guid ResponseId { get; init; }
}
