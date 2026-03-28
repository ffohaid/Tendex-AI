using MediatR;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.AuditTrail.Queries;

/// <summary>
/// Handles the <see cref="GetAuditLogByIdQuery"/> by delegating to <see cref="IAuditLogService"/>.
/// </summary>
public sealed class GetAuditLogByIdQueryHandler : IRequestHandler<GetAuditLogByIdQuery, AuditLogEntry?>
{
    private readonly IAuditLogService _auditLogService;

    public GetAuditLogByIdQueryHandler(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    public async Task<AuditLogEntry?> Handle(GetAuditLogByIdQuery request, CancellationToken cancellationToken)
    {
        return await _auditLogService.GetByIdAsync(request.Id, cancellationToken);
    }
}
