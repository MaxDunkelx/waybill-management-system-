using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Background service for consuming waybill events from RabbitMQ.
/// 
/// PURPOSE:
/// This hosted service runs in the background and continuously listens for waybill
/// events from RabbitMQ. It starts when the application starts and stops gracefully
/// when the application shuts down.
/// 
/// HOSTED SERVICE PATTERN:
/// This service inherits from BackgroundService, which is the recommended way to
/// run long-running background tasks in ASP.NET Core. The service:
/// - Starts automatically when the application starts
/// - Runs continuously in the background
/// - Stops gracefully when the application shuts down
/// - Handles cancellation tokens properly
/// 
/// LIFECYCLE:
/// 1. Application starts → ExecuteAsync() is called
/// 2. Service connects to RabbitMQ and starts consuming
/// 3. Service runs until cancellation is requested
/// 4. Application shuts down → cancellation token is triggered
/// 5. Service stops consuming and closes connection
/// 
/// ERROR HANDLING:
/// If the consumer encounters an error, it logs the error and continues running.
/// The underlying MessageConsumer handles connection recovery automatically.
/// 
/// </summary>
public class WaybillEventConsumer : BackgroundService
{
    private readonly IMessageConsumer _messageConsumer;
    private readonly ILogger<WaybillEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the WaybillEventConsumer.
    /// </summary>
    /// <param name="messageConsumer">The message consumer service.</param>
    /// <param name="logger">Logger for recording service operations.</param>
    public WaybillEventConsumer(
        IMessageConsumer messageConsumer,
        ILogger<WaybillEventConsumer> logger)
    {
        _messageConsumer = messageConsumer ?? throw new ArgumentNullException(nameof(messageConsumer));
        _logger = logger;
    }

    /// <summary>
    /// Executes the background service.
    /// 
    /// This method is called when the service starts. It begins listening for
    /// events from RabbitMQ and continues until cancellation is requested.
    /// 
    /// </summary>
    /// <param name="stoppingToken">Cancellation token that signals when to stop.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WaybillEventConsumer service is starting...");

        try
        {
            // Start listening for events
            // This will run until the cancellation token is triggered
            await _messageConsumer.StartListeningAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WaybillEventConsumer service is stopping (cancellation requested)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WaybillEventConsumer service");
            throw;
        }
        finally
        {
            // Stop listening and clean up
            await _messageConsumer.StopListeningAsync();
            _logger.LogInformation("WaybillEventConsumer service has stopped");
        }
    }
}
