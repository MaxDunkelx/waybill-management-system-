namespace WaybillManagementSystem.Services;

/// <summary>
/// Service interface for distributed locking.
/// 
/// PURPOSE:
/// This service provides distributed locking capabilities to ensure that certain
/// operations can only be executed by one process/user at a time. This is critical
/// for preventing concurrent execution of expensive operations like report generation,
/// data migrations, or batch processing.
/// 
/// USE CASES:
/// - Report generation (prevent multiple simultaneous reports)
/// - Data synchronization (prevent concurrent sync operations)
/// - Batch processing (ensure only one batch runs at a time)
/// - Scheduled tasks (prevent overlapping executions)
/// 
/// LOCK MECHANISM:
/// The service uses a key-based locking mechanism:
/// - Each operation has a unique lock key (e.g., "monthly-report-generation")
/// - Only one process can acquire a lock with a given key at a time
/// - Locks have a timeout to prevent deadlocks
/// - Locks must be explicitly released or will expire automatically
/// 
/// IMPLEMENTATION:
/// The current implementation uses in-memory locking (suitable for single-server deployments).
/// For multi-server deployments, use a Redis-based implementation to ensure locks work
/// across multiple application instances.
/// 
/// THREAD SAFETY:
/// All lock operations are thread-safe and can be used concurrently from multiple
/// threads or HTTP requests.
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Attempts to acquire a distributed lock with the specified key.
    /// 
    /// This method tries to acquire an exclusive lock for the given key. If another
    /// process already holds the lock, this method will wait up to the specified
    /// timeout before giving up.
    /// 
    /// LOCK ACQUISITION:
    /// - If the lock is available, it is acquired immediately and the method returns true
    /// - If the lock is held by another process, the method waits up to the timeout
    /// - If the lock becomes available within the timeout, it is acquired and returns true
    /// - If the timeout expires, the method returns false
    /// 
    /// TIMEOUT:
    /// The timeout parameter specifies the maximum time to wait for the lock to become
    /// available. If set to TimeSpan.Zero, the method returns immediately without waiting.
    /// 
    /// LOCK EXPIRATION:
    /// Locks automatically expire after a default duration (e.g., 5 minutes) to prevent
    /// deadlocks if a process crashes without releasing the lock. The expiration time
    /// should be longer than the expected operation duration.
    /// 
    /// </summary>
    /// <param name="lockKey">The unique key identifying the lock (e.g., "monthly-report-generation").</param>
    /// <param name="timeout">Maximum time to wait for the lock to become available. Use TimeSpan.Zero for immediate return.</param>
    /// <returns>
    /// True if the lock was successfully acquired, false if the timeout expired or lock is held by another process.
    /// </returns>
    Task<bool> AcquireLockAsync(string lockKey, TimeSpan timeout);

    /// <summary>
    /// Releases a distributed lock with the specified key.
    /// 
    /// This method releases a lock that was previously acquired. It is safe to call
    /// this method even if the lock was not acquired or has already been released.
    /// 
    /// IMPORTANT:
    /// Always release locks in a finally block to ensure they are released even if
    /// an exception occurs during the operation. This prevents deadlocks.
    /// 
    /// EXAMPLE:
    /// ```csharp
    /// if (await lockService.AcquireLockAsync("my-lock", TimeSpan.FromSeconds(5)))
    /// {
    ///     try
    ///     {
    ///         // Perform operation
    ///     }
    ///     finally
    ///     {
    ///         await lockService.ReleaseLockAsync("my-lock");
    ///     }
    /// }
    /// ```
    /// 
    /// </summary>
    /// <param name="lockKey">The unique key identifying the lock to release.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReleaseLockAsync(string lockKey);
}
