using MediatR;
using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.ActiveDirectory.Commands;

/// <summary>
/// Command to test the Active Directory connection for a tenant.
/// Records the test result in the database.
/// </summary>
public sealed record TestActiveDirectoryConnectionCommand(
    Guid TenantId) : IRequest<Result<TestConnectionResultDto>>;

/// <summary>
/// DTO for the connection test result.
/// </summary>
public sealed record TestConnectionResultDto(
    bool Success,
    string? ErrorMessage,
    DateTime TestedAt);

/// <summary>
/// Handles testing the AD connection.
/// Note: Actual LDAP connectivity test is simulated here.
/// In production, this would use an LDAP client library.
/// </summary>
public sealed class TestActiveDirectoryConnectionCommandHandler
    : IRequestHandler<TestActiveDirectoryConnectionCommand, Result<TestConnectionResultDto>>
{
    private readonly IMasterPlatformDbContext _dbContext;

    public TestActiveDirectoryConnectionCommandHandler(IMasterPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<TestConnectionResultDto>> Handle(
        TestActiveDirectoryConnectionCommand request,
        CancellationToken cancellationToken)
    {
        var config = await _dbContext.GetDbSet<ActiveDirectoryConfiguration>()
            .FirstOrDefaultAsync(c => c.TenantId == request.TenantId, cancellationToken);

        if (config is null)
            return Result.Failure<TestConnectionResultDto>(
                "لم يتم العثور على إعدادات الدليل النشط لهذه الجهة.");

        bool success;
        string? errorMessage = null;

        try
        {
            // Attempt a basic TCP connection to the AD server to verify reachability
            var uri = new Uri(config.ServerUrl);
            var host = uri.Host;
            var port = config.Port;

            using var tcpClient = new System.Net.Sockets.TcpClient();
            var connectTask = tcpClient.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                success = false;
                errorMessage = $"Connection timed out after 10 seconds to {host}:{port}";
            }
            else if (connectTask.IsFaulted)
            {
                success = false;
                errorMessage = connectTask.Exception?.InnerException?.Message
                               ?? "Connection failed with unknown error.";
            }
            else
            {
                success = true;
            }
        }
        catch (Exception ex)
        {
            success = false;
            errorMessage = ex.Message;
        }

        // Record the test result
        config.RecordConnectionTest(success, errorMessage);
        await ((DbContext)_dbContext).SaveChangesAsync(cancellationToken);

        return Result.Success(new TestConnectionResultDto(
            Success: success,
            ErrorMessage: errorMessage,
            TestedAt: DateTime.UtcNow));
    }
}
