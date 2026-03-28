using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Infrastructure.Storage.MinIO;

/// <summary>
/// Background service that runs on application startup to ensure
/// the default MinIO bucket exists and is properly configured.
/// </summary>
public sealed partial class MinioStartupInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MinioStartupInitializer> _logger;

    public MinioStartupInitializer(
        IServiceProvider serviceProvider,
        ILogger<MinioStartupInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogInitializing(_logger);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var storageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
            await storageService.EnsureBucketExistsAsync(cancellationToken: cancellationToken);
            LogInitialized(_logger);
        }
        catch (Exception ex)
        {
            LogInitializationFailed(_logger, ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Initializing MinIO storage...")]
    private static partial void LogInitializing(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "MinIO storage initialized successfully.")]
    private static partial void LogInitialized(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to initialize MinIO storage on startup. The service will retry on first use.")]
    private static partial void LogInitializationFailed(ILogger logger, Exception ex);
}
