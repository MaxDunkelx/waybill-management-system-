using Microsoft.AspNetCore.Mvc;
using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Services;

namespace WaybillManagementSystem.Controllers;

/// <summary>
/// Controller for supplier-related operations.
/// 
/// PURPOSE:
/// This controller provides endpoints for querying supplier information and statistics.
/// All endpoints automatically filter by tenant through the TenantMiddleware and
/// global query filters.
/// 
/// TENANT ISOLATION:
/// All endpoints extract the tenant ID from the request context (set by TenantMiddleware)
/// and pass it to the service layer. The ApplicationDbContext's global query filter
/// ensures that all database queries are automatically scoped to the current tenant,
/// providing defense-in-depth security.
/// 
/// ENDPOINTS:
/// - GET /api/suppliers/{id}/summary - Get comprehensive statistics for a supplier
/// 
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly IWaybillService _waybillService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<SuppliersController> _logger;

    /// <summary>
    /// Initializes a new instance of the SuppliersController.
    /// </summary>
    /// <param name="waybillService">Service for querying waybill data and statistics.</param>
    /// <param name="tenantService">Service for accessing the current tenant ID.</param>
    /// <param name="logger">Logger for recording controller operations.</param>
    public SuppliersController(
        IWaybillService waybillService,
        ITenantService tenantService,
        ILogger<SuppliersController> logger)
    {
        _waybillService = waybillService ?? throw new ArgumentNullException(nameof(waybillService));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger;
    }

    /// <summary>
    /// Gets comprehensive summary statistics for a supplier including total deliveries, quantities, amounts, averages, and status breakdown.
    /// Returns 404 if the supplier doesn't exist or belongs to a different tenant.
    /// </summary>
    /// <param name="id">The supplier ID to get summary for.</param>
    /// <returns>
    /// SupplierSummaryResponseDto containing supplier statistics.
    /// Status 200 if supplier found.
    /// Status 404 if supplier not found or belongs to different tenant.
    /// Status 500 if critical error occurred.
    /// </returns>
    [HttpGet("{id}/summary")]
    [ProducesResponseType(typeof(SupplierSummaryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SupplierSummaryResponseDto>> GetSupplierSummary(string id)
    {
        // Tenant ID is guaranteed to be available - middleware validates it before reaching controllers
        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogInformation(
            "Retrieving supplier summary for supplier {SupplierId} and tenant {TenantId}",
            id,
            tenantId);

        try
        {
            var summary = await _waybillService.GetSupplierSummaryAsync(id, tenantId);

            if (summary == null)
            {
                _logger.LogWarning(
                    "Supplier {SupplierId} not found for tenant {TenantId}",
                    id,
                    tenantId);
                return NotFound(new
                {
                    error = "Supplier not found",
                    message = $"Supplier with ID '{id}' was not found or does not belong to your tenant."
                });
            }

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier summary for supplier {SupplierId} and tenant {TenantId}", id, tenantId);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "An error occurred while retrieving the supplier summary."
            });
        }
    }
}
