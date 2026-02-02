namespace WaybillManagementSystem.Services;

/// <summary>
/// Interface for caching service.
/// 
/// PURPOSE:
/// This service provides caching capabilities for frequently accessed data,
/// improving performance by reducing database queries.
/// 
/// IMPLEMENTATION:
/// The service can be implemented using Redis (distributed cache) or
/// in-memory cache (fallback). Redis is preferred for multi-instance deployments.
/// 
/// CACHE STRATEGY:
/// - Cache keys: `waybill:summary:{tenantId}:{dateRange}`, `supplier:summary:{tenantId}:{supplierId}`
/// - TTL: 5 minutes (configurable)
/// - Invalidation: On waybill import, status update, or waybill update
/// - Tenant-scoped: All cache keys include tenant ID for isolation
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached value if found, null otherwise.</returns>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Sets a cached value with a time-to-live (TTL).
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="ttl">Time-to-live (default: 5 minutes).</param>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class;

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// Removes all cached values matching a pattern (e.g., for tenant-scoped invalidation).
    /// </summary>
    /// <param name="pattern">The key pattern to match (e.g., "waybill:summary:tenant001:*").</param>
    Task RemoveByPatternAsync(string pattern);
}
