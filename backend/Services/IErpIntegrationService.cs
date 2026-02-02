using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Models;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Interface for ERP integration service that synchronizes waybill data with Priority ERP system.
/// 
/// PURPOSE:
/// This service provides methods for synchronizing waybill data with an external Priority ERP system.
/// It handles retry logic with exponential backoff, simulates ERP failures, and tracks sync status.
/// 
/// IMPLEMENTATION:
/// The service includes a mock Priority ERP endpoint that simulates real ERP behavior:
/// - Accepts waybill data via HTTP POST
/// - Simulates 10% random failure rate
/// - Returns success/failure responses
/// 
/// RETRY LOGIC:
/// The service implements exponential backoff retry logic:
/// - Initial delay: 1 second
/// - Max retries: 3
/// - Exponential backoff: 2^n seconds (1s, 2s, 4s)
/// - Circuit breaker pattern to prevent cascading failures
/// 
/// SYNC STATUS TRACKING:
/// The service updates waybill sync status:
/// - PENDING_SYNC → SYNCED (on success)
/// - PENDING_SYNC → SYNC_FAILED (on failure after retries)
/// </summary>
public interface IErpIntegrationService
{
    /// <summary>
    /// Synchronizes a waybill with the Priority ERP system.
    /// 
    /// This method attempts to send waybill data to the ERP system with retry logic.
    /// It updates the waybill's sync status based on the result.
    /// 
    /// RETRY BEHAVIOR:
    /// - Attempts up to 3 retries with exponential backoff
    /// - Waits 1s, 2s, 4s between retries
    /// - Updates sync status on success or final failure
    /// 
    /// CIRCUIT BREAKER:
    /// If ERP is consistently failing, circuit breaker opens to prevent
    /// excessive retry attempts. Circuit breaker resets after timeout period.
    /// </summary>
    /// <param name="waybill">The waybill to synchronize with ERP.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>
    /// True if synchronization succeeded, false if it failed after all retries.
    /// </returns>
    Task<bool> SyncWaybillAsync(Waybill waybill, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current circuit breaker state for monitoring purposes.
    /// </summary>
    /// <returns>True if circuit breaker is open (ERP unavailable), false if closed (normal operation).</returns>
    bool IsCircuitBreakerOpen();
}
