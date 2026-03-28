namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Abstraction for distributed caching operations (TASK-703).
///
/// Provides a unified interface for caching frequently accessed data
/// to reduce database load. Implementations should use Redis in production
/// and in-memory cache for development.
///
/// Usage patterns:
/// - Tenant configuration caching (reduces DB lookups per request).
/// - Competition list caching with tag-based invalidation.
/// - User role/permission caching for authorization checks.
/// - AI configuration caching (provider settings, API keys).
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key. Returns null if the key does not exist or has expired.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached value, or null if not found.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a value in the cache with the specified expiration.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Absolute expiration relative to now.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets a cached value or creates it using the factory function if not found.
    /// This is the preferred method for cache-aside pattern.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Factory function to create the value if not cached.</param>
    /// <param name="expiration">Absolute expiration relative to now.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached or newly created value.</returns>
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cached values matching the specified prefix pattern.
    /// Useful for invalidating all cache entries for a specific tenant or entity type.
    /// </summary>
    /// <param name="keyPrefix">The key prefix pattern to match.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveByPrefixAsync(string keyPrefix, CancellationToken cancellationToken = default);
}
