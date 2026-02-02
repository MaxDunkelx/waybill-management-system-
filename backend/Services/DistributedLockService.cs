using System.Collections.Concurrent;

namespace WaybillManagementSystem.Services;

/// <summary>
/// In-memory implementation of distributed locking service.
/// 
/// IMPLEMENTATION DETAILS:
/// This implementation uses an in-memory ConcurrentDictionary to store locks.
/// Each lock is represented by a SemaphoreSlim that allows only one acquisition
/// at a time. The lock also has an expiration timestamp to prevent deadlocks.
/// 
/// LOCK STRUCTURE:
/// - Key: The lock key (e.g., "monthly-report-generation")
/// - Value: A tuple containing:
///   - SemaphoreSlim: Controls access to the lock
///   - DateTime: Expiration timestamp (when the lock will expire)
/// 
/// LOCK ACQUISITION:
/// 1. Get or create a SemaphoreSlim for the lock key
/// 2. Try to acquire the semaphore (with timeout)
/// 3. If acquired, set expiration timestamp
/// 4. If timeout expires, return false
/// 
/// LOCK EXPIRATION:
/// Locks automatically expire after a default duration (5 minutes) to prevent
/// deadlocks if a process crashes without releasing the lock. The expiration
/// is checked when attempting to acquire a lock - expired locks are automatically
/// cleaned up.
/// 
/// LOCK RELEASE:
/// When a lock is released:
/// 1. Find the lock in the dictionary
/// 2. Release the semaphore
/// 3. Remove the lock from the dictionary if no one is waiting
/// 
/// THREAD SAFETY:
/// - ConcurrentDictionary ensures thread-safe access to the lock dictionary
/// - SemaphoreSlim ensures thread-safe lock acquisition/release
/// - All operations are async to prevent blocking
/// 
/// LIMITATIONS:
/// This in-memory implementation only works within a single application instance.
/// For multi-server deployments, use a Redis-based implementation to ensure locks
/// work across multiple application instances.
/// 
/// PERFORMANCE:
/// - Lock acquisition is very fast (in-memory operations)
/// - No network overhead (unlike Redis-based locks)
/// - Suitable for single-server deployments or development
/// 
/// UPGRADE PATH:
/// To upgrade to Redis-based locking:
/// 1. Create RedisDistributedLockService implementing IDistributedLockService
/// 2. Use Redis SETNX (SET if Not eXists) with expiration
/// 3. Register RedisDistributedLockService in Program.cs instead of this service
/// </summary>
public class DistributedLockService : IDistributedLockService
{
    private readonly ConcurrentDictionary<string, LockInfo> _locks = new();
    private readonly ILogger<DistributedLockService> _logger;
    private static readonly TimeSpan DefaultLockExpiration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Represents a lock with its semaphore and expiration timestamp.
    /// </summary>
    private class LockInfo
    {
        public SemaphoreSlim Semaphore { get; set; } = new SemaphoreSlim(1, 1);
        public DateTime ExpirationTime { get; set; }
        public DateTime AcquiredTime { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the DistributedLockService.
    /// </summary>
    /// <param name="logger">Logger for recording lock operations.</param>
    public DistributedLockService(ILogger<DistributedLockService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to acquire a distributed lock with the specified key.
    /// 
    /// This method implements the lock acquisition logic:
    /// 1. Get or create a SemaphoreSlim for the lock key
    /// 2. Check if the lock has expired (cleanup expired locks)
    /// 3. Try to acquire the semaphore with the specified timeout
    /// 4. If acquired, set expiration timestamp and return true
    /// 5. If timeout expires, return false
    /// 
    /// </summary>
    /// <param name="lockKey">The unique key identifying the lock.</param>
    /// <param name="timeout">Maximum time to wait for the lock to become available.</param>
    /// <returns>True if the lock was successfully acquired, false otherwise.</returns>
    public async Task<bool> AcquireLockAsync(string lockKey, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(lockKey))
        {
            throw new ArgumentException("Lock key cannot be null or empty.", nameof(lockKey));
        }

        _logger.LogDebug(
            "Attempting to acquire lock '{LockKey}' with timeout {Timeout}",
            lockKey,
            timeout);

        // Get or create lock info for this key
        var lockInfo = _locks.GetOrAdd(lockKey, key =>
        {
            _logger.LogDebug("Creating new lock for key '{LockKey}'", key);
            return new LockInfo
            {
                ExpirationTime = DateTime.UtcNow.Add(DefaultLockExpiration),
                AcquiredTime = DateTime.UtcNow
            };
        });

        // Check if lock has expired (cleanup expired locks)
        if (DateTime.UtcNow > lockInfo.ExpirationTime)
        {
            _logger.LogWarning(
                "Lock '{LockKey}' has expired. Cleaning up and creating new lock.",
                lockKey);

            // Remove expired lock
            if (_locks.TryRemove(lockKey, out var expiredLock))
            {
                expiredLock.Semaphore.Dispose();
            }

            // Create new lock
            lockInfo = _locks.GetOrAdd(lockKey, key => new LockInfo
            {
                ExpirationTime = DateTime.UtcNow.Add(DefaultLockExpiration),
                AcquiredTime = DateTime.UtcNow
            });
        }

        // Try to acquire the semaphore (wait up to timeout)
        // SemaphoreSlim.WaitAsync returns true if acquired, false if timeout expired
        var acquired = await lockInfo.Semaphore.WaitAsync(timeout);

        if (acquired)
        {
            // Lock acquired successfully
            lockInfo.AcquiredTime = DateTime.UtcNow;
            lockInfo.ExpirationTime = DateTime.UtcNow.Add(DefaultLockExpiration);

            _logger.LogInformation(
                "Successfully acquired lock '{LockKey}' (expires at {ExpirationTime})",
                lockKey,
                lockInfo.ExpirationTime);
        }
        else
        {
            // Timeout expired - lock is held by another process
            _logger.LogWarning(
                "Failed to acquire lock '{LockKey}' within timeout {Timeout}. Lock is held by another process.",
                lockKey,
                timeout);
        }

        return acquired;
    }

    /// <summary>
    /// Releases a distributed lock with the specified key.
    /// 
    /// This method releases a lock that was previously acquired. It safely handles
    /// cases where the lock doesn't exist or has already been released.
    /// 
    /// </summary>
    /// <param name="lockKey">The unique key identifying the lock to release.</param>
    public Task ReleaseLockAsync(string lockKey)
    {
        if (string.IsNullOrWhiteSpace(lockKey))
        {
            throw new ArgumentException("Lock key cannot be null or empty.", nameof(lockKey));
        }

        _logger.LogDebug("Releasing lock '{LockKey}'", lockKey);

        if (_locks.TryGetValue(lockKey, out var lockInfo))
        {
            try
            {
                // Release the semaphore (allows next waiting process to acquire)
                lockInfo.Semaphore.Release();

                _logger.LogInformation("Successfully released lock '{LockKey}'", lockKey);

                // Optionally remove the lock if no one is waiting
                // For now, we keep it in the dictionary for reuse
                // In a production system, you might want to clean up unused locks periodically
            }
            catch (SemaphoreFullException)
            {
                // Semaphore was already released (shouldn't happen, but handle gracefully)
                _logger.LogWarning(
                    "Attempted to release lock '{LockKey}' but semaphore was already released",
                    lockKey);
            }
        }
        else
        {
            _logger.LogWarning(
                "Attempted to release lock '{LockKey}' but lock does not exist",
                lockKey);
        }

        return Task.CompletedTask;
    }
}
