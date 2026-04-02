using MediatR;
using TendexAI.Application.Features.Inquiries.Dtos;
using TendexAI.Domain.Entities.Inquiries;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Inquiries.Queries.GetInquiryById;

public sealed record GetInquiryByIdQuery(Guid InquiryId) : IRequest<InquiryDto?>;

public sealed class GetInquiryByIdQueryHandler : IRequestHandler<GetInquiryByIdQuery, InquiryDto?>
{
    private readonly IInquiryRepository _repository;
    private readonly ICompetitionRepository _competitionRepository;

    public GetInquiryByIdQueryHandler(
        IInquiryRepository repository,
        ICompetitionRepository competitionRepository)
    {
        _repository = repository;
        _competitionRepository = competitionRepository;
    }

    public async Task<InquiryDto?> Handle(GetInquiryByIdQuery request, CancellationToken cancellationToken)
    {
        var inquiry = await _repository.GetByIdAsync(request.InquiryId, cancellationToken);
        if (inquiry is null) return null;

        // Load competition name for display
        string? competitionName = null;
        try
        {
            var competition = await _competitionRepository.GetByIdAsync(inquiry.CompetitionId, cancellationToken);
            competitionName = competition?.ProjectNameAr;
        }
        catch
        {
            // Gracefully handle if competition is not found
        }

        return MapToDto(inquiry, competitionName);
    }

    internal static InquiryDto MapToDto(Inquiry inquiry, string? competitionName = null)
    {
        return new InquiryDto
        {
            Id = inquiry.Id,
            CompetitionId = inquiry.CompetitionId,
            CompetitionName = competitionName,
            ReferenceNumber = inquiry.ReferenceNumber,
            QuestionText = inquiry.QuestionText,
            Category = inquiry.Category.ToString(),
            Priority = inquiry.Priority.ToString(),
            Status = inquiry.Status.ToString(),
            SupplierName = inquiry.SupplierName,
            EtimadReferenceNumber = inquiry.EtimadReferenceNumber,
            ApprovedAnswer = inquiry.ApprovedAnswer,
            AssignedToUserId = inquiry.AssignedToUserId,
            AssignedToUserName = inquiry.AssignedToUserName,
            AssignedToCommitteeId = inquiry.AssignedToCommitteeId,
            SlaDeadline = inquiry.SlaDeadline,
            IsOverdue = inquiry.IsOverdue,
            AnsweredAt = inquiry.AnsweredAt,
            AnsweredBy = inquiry.AnsweredBy,
            ApprovedAt = inquiry.ApprovedAt,
            ApprovedBy = inquiry.ApprovedBy,
            RejectionReason = inquiry.RejectionReason,
            IsAiAssisted = inquiry.IsAiAssisted,
            InternalNotes = inquiry.InternalNotes,
            IsExportedToEtimad = inquiry.IsExportedToEtimad,
            ExportedToEtimadAt = inquiry.ExportedToEtimadAt,
            CreatedAt = inquiry.CreatedAt,
            CreatedBy = inquiry.CreatedBy,
            Responses = inquiry.Responses.Select(r => new InquiryResponseDto
            {
                Id = r.Id,
                AnswerText = r.AnswerText,
                IsAiGenerated = r.IsAiGenerated,
                AiConfidenceScore = r.AiConfidenceScore,
                AiModelUsed = r.AiModelUsed,
                AiSources = r.AiSources,
                IsSelected = r.IsSelected,
                CreatedAt = r.CreatedAt,
                CreatedBy = r.CreatedBy
            }).OrderByDescending(r => r.CreatedAt).ToList()
        };
    }
}
