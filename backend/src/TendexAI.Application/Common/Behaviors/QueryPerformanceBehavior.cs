using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TendexAI.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs slow-running queries for performance monitoring.
/// Any request exceeding the configured threshold (default 500ms) will be logged as a warning.
/// This enables proactive identification of performance bottlenecks in production.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The MediatR response type.</typeparam>
public sealed class QueryPerformanceBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<QueryPerformanceBehavior<TRequest, TResponse>> _logger;
    private const int SlowQueryThresholdMs = 500;

    public QueryPerformanceBehavior(ILogger<QueryPerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (elapsedMs > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request detected: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms). " +
                    "Request: {@Request}",
                    requestName,
                    elapsedMs,
                    SlowQueryThresholdMs,
                    request);
            }
            else
            {
                _logger.LogDebug(
                    "Request {RequestName} completed in {ElapsedMs}ms",
                    requestName,
                    elapsedMs);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Request {RequestName} failed after {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
