using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.OperatorDashboard.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.OperatorDashboard.Queries.GetSystemHealthStatus;

/// <summary>
/// Handles system health status checks by probing infrastructure services.
/// Returns the health status of each service along with response times.
/// </summary>
public sealed class GetSystemHealthStatusQueryHandler
    : IQueryHandler<GetSystemHealthStatusQuery, SystemHealthStatusDto>
{
    private readonly IMasterPlatformDbContext _context;

    public GetSystemHealthStatusQueryHandler(IMasterPlatformDbContext context)
    {
        _context = context;
    }

    public async Task<Result<SystemHealthStatusDto>> Handle(
        GetSystemHealthStatusQuery request,
        CancellationToken cancellationToken)
    {
        var services = new List<ServiceHealthDto>();

        // Check SQL Server connectivity
        services.Add(await CheckSqlServerAsync(cancellationToken));

        // Determine overall status
        var overallStatus = services.All(s => s.Status == "Healthy")
            ? "Healthy"
            : services.Any(s => s.Status == "Unhealthy")
                ? "Unhealthy"
                : "Degraded";

        var result = new SystemHealthStatusDto(
            OverallStatus: overallStatus,
            CheckedAt: DateTime.UtcNow,
            Services: services);

        return Result.Success(result);
    }

    private async Task<ServiceHealthDto> CheckSqlServerAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Simple connectivity check via a lightweight query
            await _context.GetDbSet<Tenant>()
                .Select(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);
            sw.Stop();

            return new ServiceHealthDto(
                ServiceName: "SQL Server",
                Status: "Healthy",
                Description: "Database connection is operational.",
                ResponseTimeMs: sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ServiceHealthDto(
                ServiceName: "SQL Server",
                Status: "Unhealthy",
                Description: $"Connection failed: {ex.Message}",
                ResponseTimeMs: sw.ElapsedMilliseconds);
        }
    }
}
