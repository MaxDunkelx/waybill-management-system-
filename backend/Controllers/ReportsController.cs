using Microsoft.AspNetCore.Mvc;
using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Services;

namespace WaybillManagementSystem.Controllers;

/// <summary>
/// Controller for report generation operations.
/// 
/// PURPOSE:
/// This controller provides endpoints for generating reports. Some report generation
/// operations are expensive and should only be executed by one user at a time to
/// prevent resource exhaustion and ensure data consistency.
/// 
/// SINGLE-USER EXECUTION PATTERN:
/// The endpoints in this controller use distributed locking to ensure that only one
/// user can execute a report generation operation at a time. This pattern prevents:
/// - Concurrent execution of expensive operations
/// - Resource exhaustion (CPU, memory, database connections)
/// - Data inconsistencies from overlapping operations
/// - Duplicate work being performed
/// 
/// HOW IT WORKS:
/// 1. When a request arrives, attempt to acquire a distributed lock
/// 2. If lock is acquired:
///    - Execute the report generation operation
///    - Release the lock in a finally block
///    - Return success response
/// 3. If lock is NOT acquired (another operation is in progress):
///    - Return 409 Conflict status
///    - Include clear error message explaining that operation is already in progress
/// 
/// LOCK TIMEOUT:
/// The lock acquisition has a timeout (e.g., 1 second) to prevent the request from
/// hanging indefinitely. If the lock cannot be acquired within the timeout, the
/// request is rejected immediately.
/// 
/// ERROR HANDLING:
/// - 409 Conflict: Another operation is already in progress
/// - 500 Internal Server Error: Critical error during report generation
/// - Locks are always released in finally blocks to prevent deadlocks
/// 
/// TENANT ISOLATION:
/// All report generation operations are scoped to the current tenant. The tenant
/// ID is extracted from the request context (set by TenantMiddleware).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IDistributedLockService _lockService;
    private readonly ITenantService _tenantService;
    private readonly IWaybillService _waybillService;
    private readonly ILogger<ReportsController> _logger;

    /// <summary>
    /// Initializes a new instance of the ReportsController.
    /// </summary>
    /// <param name="lockService">Service for distributed locking.</param>
    /// <param name="tenantService">Service for accessing the current tenant ID.</param>
    /// <param name="waybillService">Service for querying waybill data and generating reports.</param>
    /// <param name="logger">Logger for recording controller operations.</param>
    public ReportsController(
        IDistributedLockService lockService,
        ITenantService tenantService,
        IWaybillService waybillService,
        ILogger<ReportsController> logger)
    {
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _waybillService = waybillService ?? throw new ArgumentNullException(nameof(waybillService));
        _logger = logger;
    }

    /// <summary>
    /// Generates a monthly waybill report for a specific year and month using distributed locking to ensure only one report generation runs at a time.
    /// Returns 409 Conflict if another report generation is already in progress.
    /// </summary>
    /// <param name="request">The monthly report request containing year and month.</param>
    /// <returns>
    /// MonthlyReportResultDto containing comprehensive monthly statistics.
    /// Status 200 if report generated successfully.
    /// Status 400 if year/month parameters are invalid.
    /// Status 409 if another operation is already in progress.
    /// Status 500 if critical error occurred.
    /// </returns>
    [HttpPost("generate-monthly-report")]
    [ProducesResponseType(typeof(MonthlyReportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateMonthlyReport([FromBody] MonthlyReportRequestDto? request)
    {
        const string lockKey = "monthly-report-generation";
        const int lockTimeoutSeconds = 1; // Wait up to 1 second for lock

        // Tenant ID is guaranteed to be available - middleware validates it before reaching controllers
        var tenantId = _tenantService.GetCurrentTenantId();

        // Validate request
        if (request == null)
        {
            return BadRequest(new
            {
                error = "Invalid request",
                message = "Year and month are required. Please provide a request body with 'year' and 'month' properties."
            });
        }

        // Validate year and month
        if (request.Year < 2020 || request.Year > 2030)
        {
            return BadRequest(new
            {
                error = "Invalid year",
                message = "Year must be between 2020 and 2030."
            });
        }

        if (request.Month < 1 || request.Month > 12)
        {
            return BadRequest(new
            {
                error = "Invalid month",
                message = "Month must be between 1 and 12."
            });
        }

        _logger.LogInformation(
            "Monthly report generation requested for year {Year}, month {Month}, tenant {TenantId}",
            request.Year,
            request.Month,
            tenantId);

        try
        {
            // ============================================================================
            // ATTEMPT TO ACQUIRE LOCK
            // ============================================================================
            // Try to acquire the distributed lock with a short timeout
            // WHY: We want to fail fast if another operation is in progress
            // The timeout prevents the request from hanging indefinitely
            var lockAcquired = await _lockService.AcquireLockAsync(
                lockKey,
                TimeSpan.FromSeconds(lockTimeoutSeconds));

            if (!lockAcquired)
            {
                // Lock is held by another process - reject the request
                _logger.LogWarning(
                    "Monthly report generation request rejected for tenant {TenantId}: " +
                    "Another report generation is already in progress",
                    tenantId);

                return Conflict(new
                {
                    error = "Report generation is already in progress",
                    message = "Another monthly report generation is currently in progress. " +
                             "Please wait for the current operation to complete before requesting a new report. " +
                             "The operation typically takes a few seconds to complete."
                });
            }

            // ============================================================================
            // LOCK ACQUIRED - EXECUTE REPORT GENERATION
            // ============================================================================
            // IMPORTANT: Always release the lock in a finally block to ensure it's
            // released even if an exception occurs during report generation.
            // This prevents deadlocks where the lock is never released.
            try
            {
                _logger.LogInformation(
                    "Lock acquired. Starting monthly report generation for year {Year}, month {Month}, tenant {TenantId}",
                    request.Year,
                    request.Month,
                    tenantId);

                // Generate the actual monthly report
                var report = await _waybillService.GenerateMonthlyReportAsync(
                    request.Year,
                    request.Month,
                    tenantId);

                _logger.LogInformation(
                    "Monthly report generation completed for year {Year}, month {Month}, tenant {TenantId}: " +
                    "TotalWaybills={TotalWaybills}, TotalAmount={TotalAmount}",
                    request.Year,
                    request.Month,
                    tenantId,
                    report.TotalWaybills,
                    report.TotalAmount);

                return Ok(report);
            }
            finally
            {
                // ============================================================================
                // ALWAYS RELEASE LOCK
                // ============================================================================
                // Release the lock in finally block to ensure it's always released,
                // even if an exception occurs during report generation.
                // This is critical to prevent deadlocks.
                await _lockService.ReleaseLockAsync(lockKey);

                _logger.LogDebug(
                    "Lock released for monthly report generation (tenant {TenantId})",
                    tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Critical error during monthly report generation");

            // Lock should already be released in finally block above
            // But if we're here, it means the lock acquisition itself failed
            // or an error occurred before entering the try block

            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "An error occurred while generating the monthly report. Please try again later."
            });
        }
    }
}
