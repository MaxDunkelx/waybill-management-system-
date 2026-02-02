namespace WaybillManagementSystem.Exceptions;

/// <summary>
/// Exception thrown when a concurrency conflict is detected during an update operation.
/// 
/// PURPOSE:
/// This exception is thrown when optimistic locking detects that the entity being updated
/// has been modified by another user/process since it was last read by the current user.
/// 
/// OPTIMISTIC LOCKING:
/// Optimistic locking prevents lost updates by checking if the entity's version matches
/// the expected version before applying updates. If the versions don't match, it means
/// another user has modified the entity, and this exception is thrown.
/// 
/// USAGE:
/// This exception is typically caught by exception handling middleware or controller
/// action filters and converted to an HTTP 409 Conflict response with a helpful message
/// instructing the client to refresh and try again.
/// 
/// CLIENT HANDLING:
/// When a client receives a 409 Conflict response:
/// 1. Refresh the entity data (GET request)
/// 2. Show the user that another user modified it
/// 3. Display both versions (original and current) if possible
/// 4. Allow the user to review changes and update again with the new version
/// 
/// </summary>
public class ConcurrencyException : Exception
{
    /// <summary>
    /// The entity ID that caused the concurrency conflict.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    /// The entity type that caused the concurrency conflict (e.g., "Waybill").
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// Initializes a new instance of the ConcurrencyException.
    /// </summary>
    /// <param name="entityId">The entity ID that caused the conflict.</param>
    /// <param name="entityType">The entity type (e.g., "Waybill").</param>
    /// <param name="message">The error message explaining the conflict.</param>
    public ConcurrencyException(string entityId, string entityType, string message)
        : base(message)
    {
        EntityId = entityId;
        EntityType = entityType;
    }

    /// <summary>
    /// Initializes a new instance of the ConcurrencyException with a default message.
    /// </summary>
    /// <param name="entityId">The entity ID that caused the conflict.</param>
    /// <param name="entityType">The entity type (e.g., "Waybill").</param>
    public ConcurrencyException(string entityId, string entityType)
        : this(entityId, entityType, $"{entityType} with ID '{entityId}' was modified by another user. Please refresh and try again.")
    {
    }
}
