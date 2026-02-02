using Microsoft.AspNetCore.Mvc;
using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Services;

namespace WaybillManagementSystem.Controllers;

/// <summary>
/// Controller for importing waybill data from CSV files.
/// 
/// PURPOSE:
/// This controller provides an HTTP endpoint for uploading CSV files containing waybill data.
/// It handles file upload, extracts the tenant ID from the request context (set by TenantMiddleware),
/// and delegates to the import service for parsing, validation, and database operations.
/// 
/// ENDPOINT:
/// POST /api/waybills/import
/// 
/// REQUEST FORMAT:
/// - Content-Type: multipart/form-data
/// - Form field name: "file" (the CSV file)
/// - Headers: X-Tenant-ID (required, set by TenantMiddleware)
/// 
/// EXPECTED CSV FORMAT:
/// The CSV file must contain the following columns (in any order):
/// - waybill_id, waybill_date, delivery_date
/// - project_id, supplier_id
/// - product_code, product_name (Hebrew)
/// - quantity, unit (Hebrew), unit_price, total_amount
/// - currency, status
/// - vehicle_number, driver_name (Hebrew, optional)
/// - delivery_address (Hebrew), notes (Hebrew, optional)
/// 
/// The CSV file must be UTF-8 encoded to properly handle Hebrew characters.
/// 
/// RESPONSE FORMAT:
/// Returns ImportResultDto with:
/// - TotalRows: Total number of rows processed
/// - SuccessCount: Number of waybills successfully imported
/// - ErrorCount: Number of rows with errors
/// - Errors: Detailed error information for each failed row
/// - Warnings: Non-critical warnings about the import
/// - ParsedWaybills: List of successfully imported waybills
/// 
/// HTTP STATUS CODES:
/// - 200 OK: Import completed (may have errors, check ErrorCount)
/// - 400 Bad Request: Missing file, invalid request format, or missing tenant ID
/// - 500 Internal Server Error: Critical error during import
/// 
/// SECURITY:
/// - Tenant ID is extracted from X-Tenant-ID header by TenantMiddleware
/// - All waybills are automatically associated with the current tenant
/// - Database queries are automatically filtered by tenant (global query filter)
/// - Cross-tenant data access is prevented at the database level
/// 
/// USAGE EXAMPLE:
/// ```bash
/// curl -X POST \
///   -H "X-Tenant-ID: tenant-123" \
///   -F "file=@waybills.csv" \
///   http://localhost:5001/api/waybills/import
/// ```
/// 
/// ERROR HANDLING:
/// The import service follows a "best effort" strategy:
/// - Continues processing even if some rows have errors
/// - Returns all errors together for user review
/// - Allows partial imports (some rows succeed, some fail)
/// - Uses database transactions to ensure data consistency
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WaybillImportController : ControllerBase
{
    private readonly IWaybillImportService _importService;
    private readonly ITenantService _tenantService;
    private readonly IJobService _jobService;
    private readonly ILogger<WaybillImportController> _logger;

    /// <summary>
    /// Initializes a new instance of the WaybillImportController.
    /// </summary>
    /// <param name="importService">Service for importing waybill data from CSV.</param>
    /// <param name="tenantService">Service for accessing the current tenant ID.</param>
    /// <param name="logger">Logger for recording controller operations.</param>
    public WaybillImportController(
        IWaybillImportService importService,
        ITenantService tenantService,
        IJobService jobService,
        ILogger<WaybillImportController> logger)
    {
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
        _logger = logger;
    }

    /// <summary>
    /// Imports waybill data from an uploaded CSV file with UTF-8 encoding support for Hebrew text.
    /// Returns detailed import results including success count, error count, and validation errors for failed rows.
    /// </summary>
    /// <param name="file">The CSV file to import. Must be UTF-8 encoded.</param>
    /// <returns>
    /// ImportResultDto containing import results.
    /// Status 200 if import completed (even with errors).
    /// Status 400 if file is missing or invalid.
    /// Status 500 if critical error occurred.
    /// </returns>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ImportWaybills(IFormFile file)
    {
        // Validate file is present
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Import request received with no file or empty file");
            return BadRequest(new
            {
                error = "File is required",
                message = "Please upload a CSV file containing waybill data."
            });
        }

        // Validate file extension (warning only, not blocking)
        var fileName = file.FileName;
        if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Import file '{FileName}' does not have .csv extension",
                fileName);
            // Continue processing - some systems may not include extension
        }

        // Tenant ID is guaranteed to be available - middleware validates it before reaching controllers
        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogInformation(
            "Starting waybill import for tenant {TenantId}. File: {FileName}, Size: {FileSize} bytes",
            tenantId,
            fileName,
            file.Length);

        try
        {
            // Process the import
            // The import service handles:
            // 1. CSV parsing (UTF-8, Hebrew support)
            // 2. Data validation (required fields, data types, business rules)
            // 3. Database operations (upsert waybills, create projects/suppliers)
            // 4. Error collection and reporting
            var result = await _importService.ImportFromCsvAsync(file.OpenReadStream(), tenantId);

            _logger.LogInformation(
                "Waybill import completed for tenant {TenantId}. " +
                "Total: {TotalRows}, Success: {SuccessCount}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                tenantId,
                result.TotalRows,
                result.SuccessCount,
                result.ErrorCount,
                result.Warnings.Count);

            // Return results
            // Status 200 even if there are errors - the import completed, just with some failures
            // Clients should check ErrorCount to determine if import was fully successful
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Critical error during waybill import for tenant {TenantId}, file {FileName}",
                tenantId,
                fileName);

            // Return 500 for critical errors
            return StatusCode(500, new
            {
                error = "Import failed",
                message = "A critical error occurred during import. Please check the logs for details.",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Imports waybill data from an uploaded CSV file asynchronously, returning a job ID immediately.
    /// The import is processed in the background. Use GET /api/jobs/{id} to check status.
    /// </summary>
    /// <param name="file">The CSV file to import. Must be UTF-8 encoded.</param>
    /// <returns>
    /// Job ID for tracking import progress.
    /// Status 202 Accepted if job created successfully.
    /// Status 400 if file is missing or invalid.
    /// </returns>
    [HttpPost("import-async")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportWaybillsAsync(IFormFile file)
    {
        // Validate file is present
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Async import request received with no file or empty file");
            return BadRequest(new
            {
                error = "File is required",
                message = "Please upload a CSV file containing waybill data."
            });
        }

        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogInformation(
            "Creating async import job for tenant {TenantId}. File: {FileName}, Size: {FileSize} bytes",
            tenantId,
            file.FileName,
            file.Length);

        try
        {
            // Save file temporarily for background processing
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"waybill_import_{Guid.NewGuid()}.csv");
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create job with file path in input data
            var inputData = System.Text.Json.JsonSerializer.Serialize(new
            {
                filePath = tempFilePath,
                fileName = file.FileName,
                fileSize = file.Length
            });

            var jobId = await _jobService.CreateJobAsync("CSV_IMPORT", tenantId, inputData);

            _logger.LogInformation(
                "Created async import job {JobId} for tenant {TenantId}",
                jobId,
                tenantId);

            // Return job ID immediately
            return Accepted(new
            {
                jobId = jobId,
                message = "Import job created. Use GET /api/jobs/{jobId} to check status.",
                statusUrl = $"/api/jobs/{jobId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating async import job for tenant {TenantId}",
                tenantId);

            return StatusCode(500, new
            {
                error = "Job creation failed",
                message = "An error occurred while creating the import job."
            });
        }
    }
}
