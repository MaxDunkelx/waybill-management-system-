# Caching Strategy Documentation

## Overview

The Waybill Management System implements a comprehensive caching strategy to improve performance by reducing database queries for frequently accessed data. The system uses Redis for distributed caching with an in-memory fallback.

## Cache Implementation

### Cache Service Interface

The system uses `ICacheService` interface with two implementations:
- **RedisCacheService**: Distributed caching using Redis (preferred for production)
- **MemoryCacheService**: In-memory caching (fallback for single-instance deployments)

### Cache Key Strategy

All cache keys follow a consistent pattern to ensure tenant isolation and easy invalidation:

```
{entity}:{operation}:{tenantId}:{identifier}
```

**Examples:**
- `waybill:summary:TENANT001:all` - Summary for all waybills (no date filter)
- `waybill:summary:TENANT001:2024-09-01:2024-09-30` - Summary for date range
- `supplier:summary:TENANT001:SUP001` - Supplier summary
- `monthly-report:TENANT001:2024:10` - Monthly report (if cached)

### Cached Data

The following data is cached:

1. **Waybill Summary** (`GET /api/waybills/summary`)
   - Cache key: `waybill:summary:{tenantId}:{dateRange}`
   - TTL: 5 minutes
   - Invalidation: On waybill import, status update, or waybill update

2. **Supplier Summary** (`GET /api/suppliers/{id}/summary`)
   - Cache key: `supplier:summary:{tenantId}:{supplierId}`
   - TTL: 5 minutes
   - Invalidation: On waybill import, status update, or waybill update

## Cache Invalidation Strategy

### Invalidation Triggers

Cache is invalidated in the following scenarios:

1. **Waybill Import** (`POST /api/waybills/import`)
   - Invalidates: All waybill summaries and supplier summaries for the tenant
   - Pattern: `waybill:summary:{tenantId}:*`, `supplier:summary:{tenantId}:*`

2. **Waybill Status Update** (`PATCH /api/waybills/{id}/status`)
   - Invalidates: All waybill summaries and supplier summaries for the tenant
   - Pattern: `waybill:summary:{tenantId}:*`, `supplier:summary:{tenantId}:*`

3. **Waybill Update** (`PUT /api/waybills/{id}`)
   - Invalidates: All waybill summaries and supplier summaries for the tenant
   - Pattern: `waybill:summary:{tenantId}:*`, `supplier:summary:{tenantId}:*`

### Invalidation Implementation

```csharp
// After waybill import/update
await _cacheService.RemoveByPatternAsync($"waybill:summary:{tenantId}:*");
await _cacheService.RemoveByPatternAsync($"supplier:summary:{tenantId}:*");
```

**Note**: Pattern-based removal is a limitation of the current implementation. For production, consider using StackExchange.Redis directly for pattern matching (e.g., `KEYS` command with `SCAN` for better performance).

## Time-to-Live (TTL)

### Default TTL

- **Default**: 5 minutes (configurable via `Cache:DefaultTtlMinutes` in appsettings.json)
- **Rationale**: Balance between freshness and performance
- **Custom TTL**: Can be specified per cache operation if needed

### TTL Configuration

```json
{
  "Cache": {
    "DefaultTtlMinutes": 5
  }
}
```

## Tenant Isolation

All cache keys include the tenant ID to ensure:
- **Data Isolation**: Tenants cannot access each other's cached data
- **Selective Invalidation**: Cache invalidation is tenant-scoped
- **Security**: Even if cache keys are leaked, tenant ID prevents cross-tenant access

## Fallback Strategy

### Redis Unavailable

If Redis is unavailable or not configured:
1. **RedisCacheService**: Returns `null` on cache operations (graceful degradation)
2. **Application**: Continues to work without caching (slower but functional)
3. **Logging**: Warnings are logged when cache operations fail

### Single-Instance Deployment

For single-instance deployments, `MemoryCacheService` can be used:
- **Advantage**: No external dependency (Redis)
- **Limitation**: Not distributed (cache not shared across instances)
- **Usage**: Register `MemoryCacheService` instead of `RedisCacheService` in `Program.cs`

## Performance Considerations

### Cache Hit Rate

Expected cache hit rates:
- **Waybill Summary**: 70-80% (frequently accessed, changes infrequently)
- **Supplier Summary**: 60-70% (accessed less frequently)

### Cache Miss Handling

On cache miss:
1. Query database
2. Calculate result
3. Store in cache
4. Return result

### Cache Warming

Currently, cache is populated on-demand (lazy loading). For production, consider:
- Pre-warming cache on application startup
- Background refresh of frequently accessed data
- Cache warming after data imports

## Monitoring

### Cache Metrics

Monitor the following metrics:
- **Cache Hit Rate**: Percentage of requests served from cache
- **Cache Miss Rate**: Percentage of requests requiring database queries
- **Cache Size**: Memory usage (for in-memory cache)
- **Redis Connection Status**: Health of Redis connection

### Logging

Cache operations are logged at Debug level:
- Cache hits: `Retrieved {entity} from cache for tenant {tenantId}`
- Cache misses: Normal operation (no special logging)
- Cache errors: Warnings logged when cache operations fail

## Configuration

### Redis Configuration

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "WaybillManagement"
  }
}
```

### Cache Configuration

```json
{
  "Cache": {
    "DefaultTtlMinutes": 5
  }
}
```

## Future Improvements

1. **Pattern-Based Invalidation**: Implement proper pattern matching using StackExchange.Redis
2. **Cache Warming**: Pre-populate cache on startup or after imports
3. **Cache Statistics**: Add metrics for cache hit/miss rates
4. **Cache Compression**: Compress large cache values to reduce memory usage
5. **Cache Versioning**: Add version numbers to cache keys for easier invalidation
