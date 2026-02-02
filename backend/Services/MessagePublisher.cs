using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using WaybillManagementSystem.Models.Events;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Service implementation for publishing events to RabbitMQ message broker.
/// 
/// IMPLEMENTATION DETAILS:
/// This service manages RabbitMQ connections and publishes events to exchanges.
/// It handles connection management, exchange/queue setup, and message serialization.
/// 
/// RABBITMQ ARCHITECTURE:
/// RabbitMQ uses the following concepts:
/// - Exchange: Receives messages and routes them to queues
/// - Queue: Stores messages until consumed
/// - Binding: Links exchanges to queues with routing rules
/// 
/// SETUP:
/// - Exchange: "waybill-events" (topic exchange for flexible routing)
/// - Queue: "waybill-imported" (stores waybill import events)
/// - Routing Key: "waybill.imported" (identifies the event type)
/// 
/// CONNECTION MANAGEMENT:
/// - Connections are created on first use and reused for subsequent publishes
/// - Connection failures are handled gracefully (logged, not thrown)
/// - Reconnection is attempted on next publish
/// 
/// MESSAGE SERIALIZATION:
/// Events are serialized to JSON using System.Text.Json for efficient serialization
/// and compatibility with other systems.
/// 
/// ERROR HANDLING:
/// If RabbitMQ is unavailable, errors are logged but exceptions are not thrown.
/// This ensures that the main operation (e.g., waybill import) can complete
/// even if the message broker is down. Events are fire-and-forget.
/// 
/// </summary>
public class MessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MessagePublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _lock = new object();
    private const string ExchangeName = "waybill-events";
    private const string QueueName = "waybill-imported";
    private const string RoutingKey = "waybill.imported";

    /// <summary>
    /// Initializes a new instance of the MessagePublisher.
    /// </summary>
    /// <param name="configuration">Configuration for accessing RabbitMQ settings.</param>
    /// <param name="logger">Logger for recording publisher operations.</param>
    public MessagePublisher(IConfiguration configuration, ILogger<MessagePublisher> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;
    }

    /// <summary>
    /// Publishes a waybill import event to RabbitMQ.
    /// 
    /// This method handles the complete publishing flow:
    /// 1. Ensure connection to RabbitMQ
    /// 2. Declare exchange and queue
    /// 3. Serialize event to JSON
    /// 4. Publish message to exchange
    /// 
    /// </summary>
    /// <param name="event">The waybill import event to publish.</param>
    public async Task PublishWaybillsImportedEventAsync(WaybillsImportedEvent @event)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        try
        {
            _logger.LogDebug(
                "Publishing waybill import event for tenant {TenantId}: {SuccessCount} successful, {ErrorCount} errors",
                @event.TenantId,
                @event.SuccessCount,
                @event.ErrorCount);

            // Ensure connection is established
            EnsureConnection();

            // Declare exchange and queue
            DeclareExchangeAndQueue();

            // Serialize event to JSON
            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);

            // Publish message
            _channel!.BasicPublish(
                exchange: ExchangeName,
                routingKey: RoutingKey,
                basicProperties: null,
                body: body);

            _logger.LogInformation(
                "Successfully published waybill import event for tenant {TenantId}",
                @event.TenantId);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - event publishing is fire-and-forget
            // This ensures that the main operation (import) can complete even if
            // RabbitMQ is unavailable
            _logger.LogError(
                ex,
                "Failed to publish waybill import event for tenant {TenantId}. " +
                "The import operation completed successfully, but the event was not published.",
                @event.TenantId);
        }
    }

    /// <summary>
    /// Ensures a connection to RabbitMQ is established.
    /// 
    /// This method creates a connection if one doesn't exist, or reuses an existing
    /// connection. Connection failures are handled gracefully.
    /// 
    /// </summary>
    private void EnsureConnection()
    {
        lock (_lock)
        {
            if (_connection != null && _connection.IsOpen)
            {
                return; // Connection already exists and is open
            }

            try
            {
                _logger.LogInformation("Connecting to RabbitMQ...");

                // Get RabbitMQ connection settings from configuration
                var hostName = _configuration["RabbitMQ:HostName"] ?? "localhost";
                var port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672");
                var userName = _configuration["RabbitMQ:UserName"] ?? "guest";
                var password = _configuration["RabbitMQ:Password"] ?? "guest";
                var virtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/";

                // Create connection factory
                var factory = new ConnectionFactory
                {
                    HostName = hostName,
                    Port = port,
                    UserName = userName,
                    Password = password,
                    VirtualHost = virtualHost,
                    AutomaticRecoveryEnabled = true, // Automatically reconnect on failure
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10) // Retry every 10 seconds
                };

                // Create connection
                _connection = factory.CreateConnection();

                // Create channel
                _channel = _connection.CreateModel();

                _logger.LogInformation(
                    "Successfully connected to RabbitMQ at {HostName}:{Port}",
                    hostName,
                    port);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to connect to RabbitMQ. Event publishing will be skipped.");
                throw; // Re-throw to be caught by caller
            }
        }
    }

    /// <summary>
    /// Declares the exchange and queue for waybill events.
    /// 
    /// This method ensures that the exchange and queue exist in RabbitMQ.
    /// If they don't exist, they are created. If they already exist, this is a no-op.
    /// 
    /// EXCHANGE:
    /// - Name: "waybill-events"
    /// - Type: "topic" (allows flexible routing based on routing keys)
    /// - Durable: true (survives broker restarts)
    /// 
    /// QUEUE:
    /// - Name: "waybill-imported"
    /// - Durable: true (survives broker restarts)
    /// - Exclusive: false (can be accessed by multiple consumers)
    /// - Auto-delete: false (queue persists even when no consumers)
    /// 
    /// BINDING:
    /// The queue is bound to the exchange with routing key "waybill.imported"
    /// 
    /// </summary>
    private void DeclareExchangeAndQueue()
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("Channel is not initialized. Ensure connection is established.");
        }

        // Declare exchange
        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true, // Exchange survives broker restarts
            autoDelete: false);

        // Declare queue
        _channel.QueueDeclare(
            queue: QueueName,
            durable: true, // Queue survives broker restarts
            exclusive: false, // Can be accessed by multiple consumers
            autoDelete: false); // Queue persists even when no consumers

        // Bind queue to exchange
        _channel.QueueBind(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey);

        _logger.LogDebug(
            "Exchange '{ExchangeName}' and queue '{QueueName}' are ready",
            ExchangeName,
            QueueName);
    }

    /// <summary>
    /// Disposes of RabbitMQ connection and channel.
    /// </summary>
    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
