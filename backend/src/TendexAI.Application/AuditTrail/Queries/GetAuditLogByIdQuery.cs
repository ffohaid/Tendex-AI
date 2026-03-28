using MediatR;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.AuditTrail.Queries;

/// <summary>
/// Query to retrieve a single audit log entry by its unique identifier.
/// </summary>
public sealed record GetAuditLogByIdQuery(Guid Id) : IRequest<AuditLogEntry?>;
