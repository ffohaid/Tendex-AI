using MediatR;
using TendexAI.Application.Features.Inquiries.Dtos;
using TendexAI.Domain.Entities.Inquiries;

namespace TendexAI.Application.Features.Inquiries.Queries.GetInquiryStatistics;

public sealed record GetInquiryStatisticsQuery : IRequest<InquiryStatisticsDto>;

public sealed class GetInquiryStatisticsQueryHandler : IRequestHandler<GetInquiryStatisticsQuery, InquiryStatisticsDto>
{
    private readonly IInquiryRepository _repository;

    public GetInquiryStatisticsQueryHandler(IInquiryRepository repository)
    {
        _repository = repository;
    }

    public async Task<InquiryStatisticsDto> Handle(GetInquiryStatisticsQuery request, CancellationToken cancellationToken)
    {
        var stats = await _repository.GetStatisticsAsync(cancellationToken);

        return new InquiryStatisticsDto
        {
            Total = stats.Total,
            New = stats.New,
            InProgress = stats.InProgress,
            PendingApproval = stats.PendingApproval,
            Approved = stats.Approved,
            Rejected = stats.Rejected,
            Overdue = stats.Overdue,
            AverageResponseTimeHours = stats.AverageResponseTimeHours
        };
    }
}
