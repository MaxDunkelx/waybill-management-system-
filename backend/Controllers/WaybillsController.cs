using Microsoft.AspNetCore.Mvc;
using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Exceptions;
using WaybillManagementSystem.Services;

namespace WaybillManagementSystem.Controllers;

/// <summary>
/// Controller for waybill retrieval operations.
/// 
/// PURPOSE:
/// This controller provides HTTP endpoints for retrieving waybill data with
/// filtering, pagination, and search capabilities. All endpoints automatically
/// filter by tenant through the TenantMiddleware and global query filters.
/// 
/// TENANT ISOLATION:
/// All endpoints extract the tenant ID from the request context (set by TenantMiddleware)
/// and pass it to the service layer. The ApplicationDbContext's global query filter
/// ensures that all database queries are automatically scoped to the current tenant,
/// providing defense-in-depth security.
/// 
/// ENDPOINTS:
/// - GET /api/waybills - List waybills with filtering and pagination
/// - GET /api/waybills/{id} - Get single waybill by ID
/// - GET /api/projects/{projectId}/waybills - Get waybills for a specific project
/// 
/// FILTERING:
/// The list endpoint supports comprehensive filtering via query parameters:
/// - Date ranges (waybill_date, delivery_date)
/// - Status
/// - Project ID
/// - Supplier ID
/// - Product code
/// - Text search (Hebrew-aware)
/// 
/// PAGINATION:
/// The list endpoint supports pagination with PageNumber and PageSize parameters.
/// Results include pagination metadata (total count, page info) for building
/// pagination controls.
/// 
/// HEBREW TEXT SEARCH:
/// The SearchText parameter performs Unicode-aware searches in:
/// - Project name (Hebrew)
/// - Supplier name (Hebrew)
/// - Product name (Hebrew)
/// 
/// The search is case-insensitive and properly handles Hebrew Unicode characters.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WaybillsController : ControllerBase
{
    private readonly IWaybillService _waybillService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<WaybillsController> _logger;

    /// <summary>
    /// Initializes a new instance of the WaybillsController.
    /// </summary>
    /// <param name="waybillService">Service for querying waybill data.</param>
    /// <param name="tenantService">Service for accessing the current tenant ID.</param>
    /// <param name="logger">Logger for recording controller operations.</param>
    public WaybillsController(
        IWaybillService waybillService,
        ITenantService tenantService,
        ILogger<WaybillsController> logger)
    {
        _waybillService = waybillService ?? throw new ArgumentNullException(nameof(waybillService));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of waybills with optional filtering by date, status, project, supplier, product code, and Hebrew text search.
    /// Returns paginated results with metadata including total count, page number, and navigation flags.
    /// </summary>
    /// <param name="query">Query parameters for filtering and pagination.</param>
    /// <returns>
    /// PagedResultDto containing waybills and pagination metadata.
    /// Status 200 if successful.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<WaybillListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<WaybillListDto>>> GetWaybills([FromQuery] WaybillQueryDto query)
    {
        // Tenant ID is guaranteed to be available - middleware validates it before reaching controllers
        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogInformation(
            "Retrieving waybills for tenant {TenantId} with filters: Status={Status}, ProjectId={ProjectId}",
            tenantId,
            query.Status,
            query.ProjectId);

        try
        {
            var result = await _waybillService.GetAllAsync(tenantId, query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving waybills for tenant {TenantId}", tenantId);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "An error occurred while retrieving waybills."
            });
        }
    }

    /// <summary>
    /// Gets a single waybill by its ID with complete details including project name and supplier name.
    /// Returns 404 if the waybill doesn't exist or belongs to a different tenant.
    /// </summary>
    /// <param name="id">The waybill ID to retrieve.</param>
    /// <returns>
    /// WaybillDto if found.
    /// Status 200 if found.
    /// Status 404 if not found or belongs to different tenant.
    /// </returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WaybillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WaybillDto>> GetWaybill(string id)
    {
        // Tenant ID is guaranteed to be available - middleware validates it before reaching controllers
        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogDebug("Retrieving waybill {WaybillId} for tenant {TenantId}", id, tenantId);

        try
        {
            var waybill = await _waybillService.GetByIdAsync(id, tenantId);

            if (waybill == null)
            {
                _logger.LogWarning(
                    "Waybill {WaybillId} not found for tenant {TenantId}",
                    id,
                    tenantId);
                return NotFound(new
                {
                    error = "Waybill not found",
                    message = $"Waybill with ID '{id}' was not found or does not belong to your tenant."
                });
            }

            return Ok(waybill);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving waybill {WaybillId} for tenant {TenantId}", id, tenantId);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "An error occurred while retrieving the waybill."
            });
        }
    }

    /// <summary>
    /// Gets all waybills for a specific project, filtered by the current tenant.
    /// Returns an empty list if the project has no waybills or doesn't belong to the tenant.
    /// </summary>
    /// <param name="projectId">The project ID to filter by.</param>
    /// <returns>
    /// List of waybills for the specified project.
    /// Status 200 if successful (may return empty list if project has no waybills).
    /// </returns>
    [HttpGet("projects/{projectId}/waybills")]
    [ProducesResponseType(typeof(List<WaybillListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<WaybillListDto>>> GetWaybillsByProject(string projectId)
    {
        // Tenant ID is guaranteed to be available - middleware validates it before reaching controllers
        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogInformation(
            "Retrieving waybills for project {ProjectId} and tenant {TenantId}",
            projectId,
            tenantId);

        try
        {
            var waybills = await _waybillService.GetByProjectIdAsync(projectId, tenantId);
            return Ok(waybills);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving waybills for project {ProjectId} and tenant {TenantId}", projectId, tenantId);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "An error occurred while retrieving waybills for the project."
            });
        }
    }

    /// <summary>
    /// Gets comprehensive summary statistics including totals by status, monthly breakdown, top suppliers, and project totals.
    /// Optional date range parameters filter results by waybill_date.
    /// </summary>
    /// <param name="dateFrom">Optional start date for filtering (YYYY-MM-DD).</param>
    /// <param name="dateTo">Optional end date for filtering (YYYY-MM-DD).</param>
    /// <returns>
    /// WaybillSummaryDto containing all aggregated statistics.
    /// Status 200 if successful.
    /// </returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(WaybillSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WaybillSummaryDto>> GetWaybillSummary(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        // Tenant ID is guaranteed to be available - middleware validates it before reaching controllers
        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogInformation(
            "Retrieving waybill summary for tenant {TenantId} (dateFrom: {DateFrom}, dateTo: {DateTo})",
            tenantId,
            dateFrom,
            dateTo);

        try
        {
            var summary = await _waybillService.GetSummaryAsync(tenantId, dateFrom, dateTo);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving waybill summary for tenant {TenantId}", tenantId);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "An error occurred while calculating waybill summary."
            });
        }
    }

    /// <summary>
    /// Updates a waybill's status with business rule validation (allowed transitions: PENDING→DELIVERED/CANCELLED, DELIVERED→DISPUTED).
    /// Returns 400 if the status transition is invalid or the waybill doesn't belong to the current tenant.
    /// </summary>
    /// <param name="id">The waybill ID to update.</param>
    /// <param name="dto">The status update request containing new status and optional notes.</param>
    /// <returns>
    /// Updated WaybillDto if successful.
    /// Status 200 if update successful.
    /// Status 400 if status transition is invalid or validation fails.
    /// Status 404 if waybill not found or belongs to different tenant.
    /// Status 500 if critical error occurred.
    /// </returns>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(WaybillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WaybillDto>> UpdateWaybillStatus(string id, [FromBody] UpdateWaybillStatusDto dto)
    {
        // Validate input
        if (!ModelState.IsValid)
        {
            _logger.LogWarning(
                "Invalid model state for waybill status update request for waybill {WaybillId}",
                id);
            return BadRequest(ModelState);
        }

        // Tenant ID is guaranteed to be available - middleware validates it before reaching controllers
        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogInformation(
            "Updating status of waybill {WaybillId} for tenant {TenantId} to {NewStatus}",
            id,
            tenantId,
            dto.Status);

        try
        {
            var updatedWaybill = await _waybillService.UpdateStatusAsync(id, tenantId, dto);

            if (updatedWaybill == null)
            {
                _logger.LogWarning(
                    "Waybill {WaybillId} not found for tenant {TenantId}",
                    id,
                    tenantId);
                return NotFound(new
                {
                    error = "Waybill not found",
                    message = $"Waybill with ID '{id}' was not found or does not belong to your tenant."
                });
            }

            return Ok(updatedWaybill);
        }
        catch (InvalidOperationException ex)
        {
            // This exception is thrown when status transition is invalid (not tenant ID error)
            _logger.LogWarning(
                ex,
                "Invalid status transition for waybill {WaybillId}: {Message}",
                id,
                ex.Message);

            return BadRequest(new
            {
                error = "Invalid status transition",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status of waybill {WaybillId}", id);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "An error occurred while updating the waybill status."
            });
        }
    }

    /// <summary>
    /// Updates a waybill with optimistic locking using the Version field to prevent concurrent update conflicts.
    /// Returns 409 Conflict if the version doesn't match, indicating another user modified the waybill.
    /// </summary>
    /// <param name="id">The waybill ID to update.</param>
    /// <param name="dto">The update request containing new field values and Version.</param>
    /// <returns>
    /// Updated WaybillDto if successful.
    /// Status 200 if update successful.
    /// Status 400 if validation fails or Version is missing.
    /// Status 404 if waybill not found or belongs to different tenant.
    /// Status 409 if concurrent update detected (version mismatch).
    /// Status 500 if critical error occurred.
    /// </returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(WaybillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WaybillDto>> UpdateWaybill(string id, [FromBody] UpdateWaybillDto dto)
    {
        // Validate input
        if (!ModelState.IsValid)
        {
            _logger.LogWarning(
                "Invalid model state for waybill update request for waybill {WaybillId}",
                id);
            return BadRequest(ModelState);
        }

        // Validate Version is provided (required for optimistic locking)
        if (dto.Version == null || dto.Version.Length == 0)
        {
            _logger.LogWarning(
                "Waybill update request missing Version for waybill {WaybillId}",
                id);
            return BadRequest(new
            {
                error = "Version is required",
                message = "Version is required for optimistic locking. Please include the Version value from the most recent GET request."
            });
        }

        // Tenant ID is guaranteed to be available - middleware validates it before reaching controllers
        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogInformation(
            "Updating waybill {WaybillId} for tenant {TenantId}",
            id,
            tenantId);

        try
        {
            var updatedWaybill = await _waybillService.UpdateWaybillAsync(id, tenantId, dto);

            if (updatedWaybill == null)
            {
                _logger.LogWarning(
                    "Waybill {WaybillId} not found for tenant {TenantId}",
                    id,
                    tenantId);
                return NotFound(new
                {
                    error = "Waybill not found",
                    message = $"Waybill with ID '{id}' was not found or does not belong to your tenant."
                });
            }

            return Ok(updatedWaybill);
        }
        catch (ConcurrencyException ex)
        {
            // Concurrent update detected - return 409 Conflict with current waybill data
            _logger.LogWarning(
                ex,
                "Concurrency conflict for waybill {WaybillId}: {Message}",
                id,
                ex.Message);

            // Get current waybill data so client can refresh
            try
            {
                var currentWaybill = await _waybillService.GetByIdAsync(id, tenantId);

                return Conflict(new
                {
                    error = "Concurrent update detected",
                    message = ex.Message,
                    currentWaybill = currentWaybill,
                    suggestion = "The waybill was modified by another user. Please refresh the data, review the changes, and try updating again with the new version."
                });
            }
            catch
            {
                // If we can't get current waybill, just return the error
                return Conflict(new
                {
                    error = "Concurrent update detected",
                    message = ex.Message,
                    suggestion = "The waybill was modified by another user. Please refresh the data and try updating again."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating waybill {WaybillId}", id);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "An error occurred while updating the waybill."
            });
        }
    }
}
