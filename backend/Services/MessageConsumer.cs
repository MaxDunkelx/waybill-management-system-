using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WaybillManagementSystem.Models.Events;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Service implementation for consuming events from RabbitMQ message broker.
/// 
/// IMPLEMENTATION DETAILS:
/// This service subscribes to RabbitMQ queues and processes incoming events.
/// It handles connection management, message deserialization, and event processing.
/// 
/// CONSUMER PATTERN:
/// The consumer pattern allows asynchronous event processing:
/// 1. Subscribe to queue
/// 2. Receive events as they arrive
/// 3. Deserialize event from JSON
/// 4. Process event (logging, notifications, etc.)
/// 5. Acknowledge message (remove from queue)
/// 
/// ERROR HANDLING:
/// If event processing fails:
/// - Error is logged
/// - Message is rejected and requeued (allows retry)
/// - After multiple failures, message can be sent to dead-letter queue
/// 
/// MESSAGE ACKNOWLEDGMENT:
/// Messages are acknowledged after successful processing. If processing fails,
/// the message is rejected and requeued for retry. This ensures no messages are lost.
/// 
/// </summary>
public class MessageConsumer : IMessageConsumer, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MessageConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private EventingBasicConsumer? _consumer;
    private const string ExchangeName = "waybill-events";
    private const string QueueName = "waybill-imported";
    private const string RoutingKey = "waybill.imported";

    /// <summary>
    /// Initializes a new instance of the MessageConsumer.
    /// </summary>
    /// <param name="configuration">Configuration for accessing RabbitMQ settings.</param>
    /// <param name="logger">Logger for recording consumer operations.</param>
    public MessageConsumer(IConfiguration configuration, ILogger<MessageConsumer> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;
    }

    /// <summary>
    /// Starts listening for events from RabbitMQ.
    /// 
    /// This method establishes a connection, declares exchange/queue, and begins
    /// consuming messages. It runs continuously until the cancellation token is triggered.
    /// 
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop listening.</param>
    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting RabbitMQ consumer...");

            // Connect to RabbitMQ
            await ConnectAsync();

            // Declare exchange and queue
            DeclareExchangeAndQueue();

            // Create consumer
            _consumer = new EventingBasicConsumer(_channel);

            // Register event handler
            _consumer.Received += async (model, ea) =>
            {
                await HandleMessageAsync(ea);
            };

            // Start consuming
            _channel!.BasicConsume(
                queue: QueueName,
                autoAck: false, // Manual acknowledgment - we'll ack after processing
                consumer: _consumer);

            _logger.LogInformation(
                "RabbitMQ consumer started. Listening for events on queue '{QueueName}'",
                QueueName);

            // Wait for cancellation
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RabbitMQ consumer stopped (cancellation requested)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RabbitMQ consumer");
            throw;
        }
    }

    /// <summary>
    /// Stops listening for events from RabbitMQ.
    /// 
    /// This method gracefully stops consuming messages and closes the connection.
    /// 
    /// </summary>
    public Task StopListeningAsync()
    {
        _logger.LogInformation("Stopping RabbitMQ consumer...");

        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();

        _logger.LogInformation("RabbitMQ consumer stopped");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Connects to RabbitMQ and creates a channel.
    /// 
    /// </summary>
    private Task ConnectAsync()
    {
        try
        {
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
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            // Create connection
            _connection = factory.CreateConnection();

            // Create channel
            _channel = _connection.CreateModel();

            _logger.LogInformation(
                "Connected to RabbitMQ at {HostName}:{Port}",
                hostName,
                port);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    /// <summary>
    /// Declares the exchange and queue for waybill events.
    /// 
    /// </summary>
    private void DeclareExchangeAndQueue()
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("Channel is not initialized");
        }

        // Declare exchange
        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Declare queue
        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

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
    /// Handles an incoming message from RabbitMQ.
    /// 
    /// This method deserializes the event and processes it. After successful
    /// processing, the message is acknowledged. If processing fails, the message
    /// is rejected and requeued for retry.
    /// 
    /// </summary>
    /// <param name="ea">Event arguments containing the message.</param>
    private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
    {
        var deliveryTag = ea.DeliveryTag;
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        try
        {
            _logger.LogDebug("Received message from RabbitMQ: {Message}", message);

            // Deserialize event
            var @event = JsonSerializer.Deserialize<WaybillsImportedEvent>(message);

            if (@event == null)
            {
                _logger.LogWarning("Failed to deserialize waybill import event. Message: {Message}", message);
                _channel!.BasicNack(deliveryTag, false, false); // Reject and don't requeue
                return;
            }

            // Process event
            await ProcessWaybillsImportedEventAsync(@event);

            // Acknowledge message (remove from queue)
            _channel!.BasicAck(deliveryTag, false);

            _logger.LogInformation(
                "Successfully processed waybill import event for tenant {TenantId}",
                @event.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing message from RabbitMQ. Message: {Message}",
                message);

            // Reject and requeue for retry
            _channel!.BasicNack(deliveryTag, false, true);
        }
    }

    /// <summary>
    /// Processes a waybill import event.
    /// 
    /// This method handles the business logic for processing the event.
    /// Currently, it logs the event. In the future, it could:
    /// - Update statistics
    /// - Send notifications
    /// - Trigger analytics
    /// - Sync to external systems
    /// 
    /// </summary>
    /// <param name="event">The waybill import event to process.</param>
    private async Task ProcessWaybillsImportedEventAsync(WaybillsImportedEvent @event)
    {
        _logger.LogInformation(
            "Processing waybill import event: Tenant={TenantId}, Imported={ImportedCount}, " +
            "Success={SuccessCount}, Errors={ErrorCount}, Timestamp={Timestamp}",
            @event.TenantId,
            @event.ImportedCount,
            @event.SuccessCount,
            @event.ErrorCount,
            @event.Timestamp);

        // TODO: Add additional processing:
        // - Update statistics in database
        // - Send email/SMS notifications
        // - Trigger analytics processing
        // - Sync to external systems

        await Task.CompletedTask; // Placeholder for async operations
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
