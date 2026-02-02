using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace WaybillManagementSystem.Services;

/// <summary>
/// In-memory implementation of the caching service (fallback when Redis is unavailable).
/// 
/// IMPLEMENTATION DETAILS:
/// This service uses IMemoryCache to provide in-memory caching capabilities.
/// It's used as a fallback when Redis is not available or for single-instance deployments.
/// 
/// LIMITATIONS:
/// - Not distributed (only works within a single application instance)
/// - Memory is limited (may evict entries under memory pressure)
/// - Pattern-based removal is not supported (limitation of IMemoryCache)
/// 
/// USAGE:
/// This service is registered as a fallback when Redis is not configured.
/// For production multi-instance deployments, RedisCacheService should be used.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the MemoryCacheService.
    /// </summary>
    public MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (_memoryCache.TryGetValue(key, out var value) && value is T typedValue)
        {
            return Task.FromResult<T?>(typedValue);
        }

        return Task.FromResult<T?>(null);
    }

    /// <summary>
    /// Sets a cached value with a time-to-live (TTL).
    /// </summary>
    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? _defaultTtl
        };

        _memoryCache.Set(key, value, options);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes all cached values matching a pattern.
    /// 
    /// NOTE: IMemoryCache doesn't support pattern matching.
    /// This is a limitation - individual keys must be removed.
    /// </summary>
    public Task RemoveByPatternAsync(string pattern)
    {
        _logger.LogWarning(
            "Pattern-based cache invalidation not supported by IMemoryCache. " +
            "Pattern: {Pattern}. Individual keys must be removed.",
            pattern);
        
        return Task.CompletedTask;
    }
}
