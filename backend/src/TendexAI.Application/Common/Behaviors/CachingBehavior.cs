using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Application.Common.Behaviors;

/// <summary>
/// Marker interface for cacheable queries (TASK-703).
///
/// Implement this interface on MediatR query requests to enable
/// automatic caching via the CachingBehavior pipeline.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// The cache key for this query. Should be unique per query parameters.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// The cache expiration duration.
    /// </summary>
    TimeSpan CacheExpiration { get; }
}

/// <summary>
/// Marker interface for cache-invalidating commands (TASK-703).
///
/// Implement this interface on MediatR command requests to automatically
/// invalidate related cache entries after successful execution.
/// </summary>
public interface ICacheInvalidatingCommand
{
    /// <summary>
    /// The cache keys to invalidate after successful command execution.
    /// </summary>
    IReadOnlyList<string> CacheKeysToInvalidate { get; }
}

/// <summary>
/// MediatR pipeline behavior that implements automatic caching for queries (TASK-703).
///
/// For queries implementing <see cref="ICacheableQuery"/>:
/// - Checks cache before executing the handler.
/// - On cache hit, returns cached result without hitting the database.
/// - On cache miss, executes the handler and stores the result in cache.
///
/// This reduces database load for frequently accessed, read-heavy endpoints
/// such as competition lists, tenant configurations, and dashboard statistics.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The MediatR response type.</typeparam>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cacheService, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only apply caching for ICacheableQuery requests
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next();
        }

        var cacheKey = cacheableQuery.CacheKey;

        // Try to get from cache
        var cachedResponse = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cachedResponse is not null)
        {
            _logger.LogDebug(
                "Cache hit for query {QueryType} with key {CacheKey}",
                typeof(TRequest).Name,
                cacheKey);
            return cachedResponse;
        }

        // Cache miss: execute handler
        _logger.LogDebug(
            "Cache miss for query {QueryType} with key {CacheKey}. Executing handler.",
            typeof(TRequest).Name,
            cacheKey);

        var response = await next();

        // Store result in cache
        if (response is not null)
        {
            await _cacheService.SetAsync(
                cacheKey,
                response,
                cacheableQuery.CacheExpiration,
                cancellationToken);
        }

        return response!;
    }
}

/// <summary>
/// MediatR pipeline behavior that invalidates cache entries after command execution (TASK-703).
///
/// For commands implementing <see cref="ICacheInvalidatingCommand"/>:
/// - Executes the command handler first.
/// - On success, invalidates all specified cache keys.
///
/// This ensures cache consistency when data is modified.
/// </summary>
/// <typeparam name="TRequest">The MediatR request type.</typeparam>
/// <typeparam name="TResponse">The MediatR response type.</typeparam>
public sealed class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger;

    public CacheInvalidationBehavior(ICacheService cacheService, ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Execute the command handler first
        var response = await next();

        // Only invalidate cache for ICacheInvalidatingCommand requests
        if (request is ICacheInvalidatingCommand invalidatingCommand)
        {
            foreach (var key in invalidatingCommand.CacheKeysToInvalidate)
            {
                await _cacheService.RemoveAsync(key, cancellationToken);
                _logger.LogDebug(
                    "Cache invalidated for key {CacheKey} after command {CommandType}",
                    key,
                    typeof(TRequest).Name);
            }
        }

        return response;
    }
}
