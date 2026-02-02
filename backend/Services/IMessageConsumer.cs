namespace WaybillManagementSystem.Services;

/// <summary>
/// Service interface for consuming events from RabbitMQ message broker.
/// 
/// PURPOSE:
/// This service provides a clean abstraction for consuming events from RabbitMQ.
/// It encapsulates the details of RabbitMQ connection, queue subscription, and
/// message deserialization, allowing other services to consume events without
/// directly depending on RabbitMQ implementation details.
/// 
/// CONSUMER PATTERN:
/// The consumer pattern allows services to react to events asynchronously:
/// - Events are published to RabbitMQ
/// - Consumer subscribes to queue and receives events
/// - Consumer processes events (logging, notifications, etc.)
/// - Events are acknowledged after successful processing
/// 
/// ERROR HANDLING:
/// If event processing fails, the event can be:
/// - Rejected and requeued (retry)
/// - Rejected and sent to dead-letter queue (permanent failure)
/// - Acknowledged anyway (fire-and-forget, log error)
/// 
/// </summary>
public interface IMessageConsumer
{
    /// <summary>
    /// Starts listening for events from RabbitMQ.
    /// 
    /// This method establishes a connection to RabbitMQ and begins consuming
    /// messages from the configured queue. It runs continuously until stopped.
    /// 
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop listening.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartListeningAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops listening for events from RabbitMQ.
    /// 
    /// This method gracefully stops consuming messages and closes the connection.
    /// 
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopListeningAsync();
}
