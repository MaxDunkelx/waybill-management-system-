using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Redis-based implementation of the caching service.
/// 
/// IMPLEMENTATION DETAILS:
/// This service uses IDistributedCache (backed by Redis) to provide distributed
/// caching capabilities. If Redis is unavailable, it falls back gracefully.
/// 
/// SERIALIZATION:
/// Values are serialized to JSON using System.Text.Json for storage in Redis.
/// 
/// CACHE KEY STRATEGY:
/// - Format: `{prefix}:{tenantId}:{identifier}`
/// - Examples:
///   - `waybill:summary:TENANT001:2024-09-01:2024-09-30`
///   - `supplier:summary:TENANT001:SUP001`
///   - `monthly-report:TENANT001:2024:10`
/// 
/// TTL STRATEGY:
/// Default TTL is 5 minutes, but can be customized per cache operation.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the RedisCacheService.
    /// </summary>
    public RedisCacheService(
        IDistributedCache distributedCache,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var cachedBytes = await _distributedCache.GetAsync(key);
            if (cachedBytes == null || cachedBytes.Length == 0)
            {
                return null;
            }

            var json = System.Text.Encoding.UTF8.GetString(cachedBytes);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving cache value for key {Key}", key);
            return null; // Graceful fallback - return null if cache is unavailable
        }
    }

    /// <summary>
    /// Sets a cached value with a time-to-live (TTL).
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? _defaultTtl
            };

            await _distributedCache.SetAsync(key, bytes, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache value for key {Key}", key);
            // Graceful fallback - continue without caching if Redis is unavailable
        }
    }

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cache value for key {Key}", key);
            // Graceful fallback - continue if cache removal fails
        }
    }

    /// <summary>
    /// Removes all cached values matching a pattern.
    /// 
    /// NOTE: IDistributedCache doesn't support pattern matching directly.
    /// This implementation would require Redis-specific operations or
    /// maintaining a key registry. For now, this is a placeholder that
    /// logs a warning. In production, you might want to use StackExchange.Redis
    /// directly for pattern-based operations.
    /// </summary>
    public Task RemoveByPatternAsync(string pattern)
    {
        _logger.LogWarning(
            "Pattern-based cache invalidation not fully implemented. " +
            "Pattern: {Pattern}. Consider using StackExchange.Redis for pattern matching.",
            pattern);
        
        // TODO: Implement pattern-based removal using StackExchange.Redis if needed
        // For now, this is a limitation - individual keys must be removed
        return Task.CompletedTask;
    }
}
