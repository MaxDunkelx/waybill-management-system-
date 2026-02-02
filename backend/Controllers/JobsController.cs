using Microsoft.AspNetCore.Mvc;
using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Services;

namespace WaybillManagementSystem.Controllers;

/// <summary>
/// Controller for job status queries.
/// 
/// PURPOSE:
/// This controller provides endpoints for querying background job status.
/// Clients can poll this endpoint to check the status of long-running operations
/// like CSV imports.
/// 
/// ENDPOINTS:
/// - GET /api/jobs/{id} - Get job status by ID
/// 
/// TENANT ISOLATION:
/// All endpoints automatically filter by tenant through the TenantMiddleware and
/// global query filters. Tenants can only see their own jobs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<JobsController> _logger;

    /// <summary>
    /// Initializes a new instance of the JobsController.
    /// </summary>
    public JobsController(
        IJobService jobService,
        ITenantService tenantService,
        ILogger<JobsController> logger)
    {
        _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger;
    }

    /// <summary>
    /// Gets job status by ID.
    /// Returns 404 if the job doesn't exist or belongs to a different tenant.
    /// </summary>
    /// <param name="id">The job ID to retrieve.</param>
    /// <returns>
    /// JobDto containing job status and results.
    /// Status 200 if job found.
    /// Status 404 if job not found or belongs to different tenant.
    /// </returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobDto>> GetJob(string id)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        _logger.LogDebug("Retrieving job {JobId} for tenant {TenantId}", id, tenantId);

        var job = await _jobService.GetJobAsync(id, tenantId);

        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found for tenant {TenantId}", id, tenantId);
            return NotFound(new
            {
                error = "Job not found",
                message = $"Job with ID '{id}' was not found or does not belong to your tenant."
            });
        }

        return Ok(job);
    }
}
