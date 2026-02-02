using WaybillManagementSystem.Models.Events;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Service interface for publishing events to RabbitMQ message broker.
/// 
/// PURPOSE:
/// This service provides a clean abstraction for publishing events to RabbitMQ.
/// It encapsulates the details of RabbitMQ connection, exchange/queue setup, and
/// message serialization, allowing other services to publish events without
/// directly depending on RabbitMQ implementation details.
/// 
/// EVENT-DRIVEN ARCHITECTURE:
/// This service enables an event-driven architecture where:
/// - Services publish events when important actions occur
/// - Other services subscribe to events and react accordingly
/// - Services are decoupled - they don't need direct references
/// - Events can trigger multiple actions asynchronously
/// 
/// RABBITMQ SETUP:
/// The implementation handles:
/// - Connection management (connect, reconnect on failure)
/// - Exchange and queue declaration
/// - Message serialization (JSON)
/// - Error handling and retry logic
/// 
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a waybill import event to RabbitMQ.
    /// 
    /// This method serializes the event to JSON and publishes it to the configured
    /// RabbitMQ exchange. The event will be delivered to all subscribed consumers.
    /// 
    /// PUBLISHING FLOW:
    /// 1. Serialize event to JSON
    /// 2. Connect to RabbitMQ (if not already connected)
    /// 3. Ensure exchange and queue exist
    /// 4. Publish message to exchange
    /// 5. Handle any connection errors gracefully
    /// 
    /// ERROR HANDLING:
    /// If publishing fails (e.g., RabbitMQ is unavailable), the error is logged
    /// but does not throw an exception. This ensures that the import operation
    /// can complete even if the message broker is down. The event is fire-and-forget.
    /// 
    /// </summary>
    /// <param name="event">The waybill import event to publish.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishWaybillsImportedEventAsync(WaybillsImportedEvent @event);
}
