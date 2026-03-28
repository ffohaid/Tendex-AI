using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.OperatorDashboard.Dtos;

namespace TendexAI.Application.Features.OperatorDashboard.Queries.GetSystemHealthStatus;

/// <summary>
/// Query to retrieve the current system health status.
/// Checks connectivity to all infrastructure services (SQL Server, Redis, RabbitMQ, MinIO, Qdrant).
/// </summary>
public sealed record GetSystemHealthStatusQuery() : IQuery<SystemHealthStatusDto>;
