using Microsoft.AspNetCore.Mvc;
using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Services;

namespace WaybillManagementSystem.Controllers;

/// <summary>
/// Controller for project-related operations.
/// 
/// PURPOSE:
/// This controller provides endpoints for querying project information and waybills.
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
/// - GET /api/projects/{id}/waybills - Get all waybills for a project
/// 
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IWaybillService _waybillService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ProjectsController> _logger;

    /// <summary>
    /// Initializes a new instance of the ProjectsController.
    /// </summary>
    /// <param name="waybillService">Service for querying waybill data.</param>
    /// <param name="tenantService">Service for accessing the current tenant ID.</param>
    /// <param name="logger">Logger for recording controller operations.</param>
    public ProjectsController(
        IWaybillService waybillService,
        ITenantService tenantService,
        ILogger<ProjectsController> logger)
    {
        _waybillService = waybillService ?? throw new ArgumentNullException(nameof(waybillService));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger;
    }

    /// <summary>
    /// Gets all waybills for a specific project, filtered by the current tenant.
    /// Returns an empty list if the project has no waybills or doesn't belong to the tenant.
    /// </summary>
    /// <param name="id">The project ID to filter by.</param>
    /// <returns>
    /// List of waybills for the specified project.
    /// Status 200 if successful (may return empty list if project has no waybills).
    /// </returns>
    [HttpGet("{id}/waybills")]
    [ProducesResponseType(typeof(List<WaybillListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<WaybillListDto>>> GetProjectWaybills(string id)
    {
        // Tenant ID is guaranteed to be available - middleware validates it before reaching controllers
        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogInformation(
            "Retrieving waybills for project {ProjectId} and tenant {TenantId}",
            id,
            tenantId);

        try
        {
            var waybills = await _waybillService.GetByProjectIdAsync(id, tenantId);
            return Ok(waybills);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving waybills for project {ProjectId} and tenant {TenantId}", id, tenantId);
            return StatusCode(500, new
            {
                error = "Internal server error",
                message = "An error occurred while retrieving waybills for the project."
            });
        }
    }
}
