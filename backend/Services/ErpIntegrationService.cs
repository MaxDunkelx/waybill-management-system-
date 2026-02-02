using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using WaybillManagementSystem.Data;
using WaybillManagementSystem.Models;
using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Service implementation for ERP integration with Priority ERP system.
/// 
/// IMPLEMENTATION DETAILS:
/// This service synchronizes waybill data with a mock Priority ERP endpoint.
/// It implements retry logic with exponential backoff and circuit breaker pattern
/// to handle ERP failures gracefully.
/// 
/// RETRY LOGIC:
/// - Initial delay: 1 second
/// - Max retries: 3
/// - Exponential backoff: 2^n seconds (1s, 2s, 4s)
/// - Circuit breaker opens after 5 consecutive failures
/// - Circuit breaker resets after 60 seconds
/// 
/// MOCK ERP ENDPOINT:
/// The service calls a mock ERP endpoint that simulates real ERP behavior:
/// - Accepts waybill data via HTTP POST
/// - Simulates 10% random failure rate
/// - Returns success/failure responses
/// 
/// SYNC STATUS TRACKING:
/// The service updates waybill sync status in the database:
/// - PENDING_SYNC → SYNCED (on success)
/// - PENDING_SYNC → SYNC_FAILED (on failure after retries)
/// </summary>
public class ErpIntegrationService : IErpIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ErpIntegrationService> _logger;
    private readonly string _erpEndpointUrl;
    
    // Circuit breaker state
    private bool _circuitBreakerOpen = false;
    private DateTime _circuitBreakerOpenedAt = DateTime.MinValue;
    private int _consecutiveFailures = 0;
    private const int CircuitBreakerFailureThreshold = 5;
    private const int CircuitBreakerResetSeconds = 60;

    /// <summary>
    /// Initializes a new instance of the ErpIntegrationService.
    /// </summary>
    /// <param name="httpClient">HTTP client for making ERP API calls.</param>
    /// <param name="dbContext">Database context for updating waybill sync status.</param>
    /// <param name="logger">Logger for recording sync operations.</param>
    /// <param name="configuration">Configuration for ERP endpoint URL.</param>
    public ErpIntegrationService(
        HttpClient httpClient,
        ApplicationDbContext dbContext,
        ILogger<ErpIntegrationService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Get ERP endpoint URL from configuration (defaults to mock endpoint)
        _erpEndpointUrl = configuration["ErpIntegration:EndpointUrl"] 
            ?? "http://localhost:5001/api/MockErp/sync-waybill";
        
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Synchronizes a waybill with the Priority ERP system.
    /// 
    /// This method attempts to send waybill data to the ERP system with retry logic
    /// and exponential backoff. It updates the waybill's sync status based on the result.
    /// </summary>
    /// <param name="waybill">The waybill to synchronize with ERP.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if synchronization succeeded, false if it failed after all retries.</returns>
    public async Task<bool> SyncWaybillAsync(Waybill waybill, CancellationToken cancellationToken = default)
    {
        if (waybill == null)
        {
            throw new ArgumentNullException(nameof(waybill));
        }

        // Check circuit breaker
        if (_circuitBreakerOpen)
        {
            // Check if circuit breaker should reset
            if (DateTime.UtcNow - _circuitBreakerOpenedAt > TimeSpan.FromSeconds(CircuitBreakerResetSeconds))
            {
                _logger.LogInformation("Circuit breaker reset after timeout period");
                _circuitBreakerOpen = false;
                _consecutiveFailures = 0;
            }
            else
            {
                _logger.LogWarning(
                    "Circuit breaker is open. Skipping ERP sync for waybill {WaybillId}",
                    waybill.Id);
                return false;
            }
        }

        const int maxRetries = 3;
        int attempt = 0;

        while (attempt < maxRetries)
        {
            attempt++;
            
            try
            {
                // Update last sync attempt timestamp
                waybill.LastErpSyncAttemptAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Prepare waybill data for ERP
                var erpData = new
                {
                    waybillId = waybill.Id,
                    waybillDate = waybill.WaybillDate.ToString("yyyy-MM-dd"),
                    deliveryDate = waybill.DeliveryDate.ToString("yyyy-MM-dd"),
                    projectId = waybill.ProjectId,
                    supplierId = waybill.SupplierId,
                    productCode = waybill.ProductCode,
                    productName = waybill.ProductName,
                    quantity = waybill.Quantity,
                    unit = waybill.Unit,
                    unitPrice = waybill.UnitPrice,
                    totalAmount = waybill.TotalAmount,
                    currency = waybill.Currency,
                    status = waybill.Status.ToString(),
                    vehicleNumber = waybill.VehicleNumber,
                    driverName = waybill.DriverName,
                    deliveryAddress = waybill.DeliveryAddress,
                    notes = waybill.Notes
                };

                _logger.LogInformation(
                    "Attempting ERP sync for waybill {WaybillId} (attempt {Attempt}/{MaxRetries})",
                    waybill.Id,
                    attempt,
                    maxRetries);

                // Send waybill data to ERP endpoint
                var response = await _httpClient.PostAsJsonAsync(
                    _erpEndpointUrl,
                    erpData,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    // Success - update waybill sync status
                    waybill.ErpSyncStatus = ErpSyncStatus.Synced;
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "Successfully synced waybill {WaybillId} with ERP after {Attempt} attempt(s)",
                        waybill.Id,
                        attempt,
                        maxRetries);

                    // Reset consecutive failures on success
                    _consecutiveFailures = 0;
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "ERP sync failed for waybill {WaybillId} (attempt {Attempt}/{MaxRetries}): " +
                        "Status {StatusCode}, Response: {ErrorContent}",
                        waybill.Id,
                        attempt,
                        maxRetries,
                        (int)response.StatusCode,
                        errorContent);

                    // If not the last attempt, wait with exponential backoff
                    if (attempt < maxRetries)
                    {
                        var delaySeconds = (int)Math.Pow(2, attempt - 1); // 1s, 2s, 4s
                        _logger.LogInformation(
                            "Waiting {DelaySeconds} seconds before retry for waybill {WaybillId}",
                            delaySeconds,
                            waybill.Id);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP error during ERP sync for waybill {WaybillId} (attempt {Attempt}/{MaxRetries})",
                    waybill.Id,
                    attempt);

                // If not the last attempt, wait with exponential backoff
                if (attempt < maxRetries)
                {
                    var delaySeconds = (int)Math.Pow(2, attempt - 1); // 1s, 2s, 4s
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Timeout during ERP sync for waybill {WaybillId} (attempt {Attempt}/{MaxRetries})",
                    waybill.Id,
                    attempt);

                // If not the last attempt, wait with exponential backoff
                if (attempt < maxRetries)
                {
                    var delaySeconds = (int)Math.Pow(2, attempt - 1); // 1s, 2s, 4s
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during ERP sync for waybill {WaybillId} (attempt {Attempt}/{MaxRetries})",
                    waybill.Id,
                    attempt);

                // If not the last attempt, wait with exponential backoff
                if (attempt < maxRetries)
                {
                    var delaySeconds = (int)Math.Pow(2, attempt - 1); // 1s, 2s, 4s
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
            }
        }

        // All retries failed - update waybill sync status to failed
        waybill.ErpSyncStatus = ErpSyncStatus.SyncFailed;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Failed to sync waybill {WaybillId} with ERP after {MaxRetries} attempts. Status set to SYNC_FAILED",
            waybill.Id,
            maxRetries);

        // Update circuit breaker state
        _consecutiveFailures++;
        if (_consecutiveFailures >= CircuitBreakerFailureThreshold)
        {
            _circuitBreakerOpen = true;
            _circuitBreakerOpenedAt = DateTime.UtcNow;
            _logger.LogError(
                "Circuit breaker opened after {ConsecutiveFailures} consecutive failures",
                _consecutiveFailures);
        }

        return false;
    }

    /// <summary>
    /// Gets the current circuit breaker state for monitoring purposes.
    /// </summary>
    /// <returns>True if circuit breaker is open (ERP unavailable), false if closed (normal operation).</returns>
    public bool IsCircuitBreakerOpen()
    {
        // Check if circuit breaker should reset
        if (_circuitBreakerOpen && 
            DateTime.UtcNow - _circuitBreakerOpenedAt > TimeSpan.FromSeconds(CircuitBreakerResetSeconds))
        {
            _circuitBreakerOpen = false;
            _consecutiveFailures = 0;
        }

        return _circuitBreakerOpen;
    }
}
