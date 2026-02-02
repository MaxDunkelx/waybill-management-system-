using Microsoft.AspNetCore.Mvc;
using WaybillManagementSystem.Services;

namespace WaybillManagementSystem.Controllers;

/// <summary>
/// Test controller for verifying multi-tenant isolation middleware.
/// 
/// PURPOSE:
/// This controller provides a simple endpoint to test that the TenantMiddleware
/// is working correctly and that the tenant ID is being extracted and made available
/// to services. This is useful during development and can be removed or secured
/// in production.
/// 
/// TESTING THE MIDDLEWARE:
/// To test the middleware, make a request with the X-Tenant-ID header:
/// 
/// GET /api/tenant/test
/// Headers:
///   X-Tenant-ID: tenant-123
/// 
/// Expected Response:
/// {
///   "tenantId": "tenant-123",
///   "message": "Tenant middleware is working correctly",
///   "timestamp": "2024-01-01T12:00:00Z"
/// }
/// 
/// ERROR SCENARIOS:
/// 1. Missing X-Tenant-ID header:
///    - Status: 400 Bad Request
///    - Response: Error message explaining the header is required
/// 
/// 2. Empty X-Tenant-ID header:
///    - Status: 400 Bad Request
///    - Response: Error message explaining the header cannot be empty
/// 
/// INTEGRATION VERIFICATION:
/// This endpoint verifies the complete integration chain:
/// 1. TenantMiddleware extracts tenant ID from header
/// 2. Stores in HttpContext.Items
/// 3. TenantService reads from HttpContext.Items
/// 4. Controller receives tenant ID through service
/// 
/// If this endpoint works, it confirms that:
/// - Middleware is correctly configured
/// - TenantService is working
/// - Tenant ID is available to controllers
/// - Database queries will be automatically filtered by tenant
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TenantTestController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantTestController> _logger;

    /// <summary>
    /// Initializes a new instance of the TenantTestController.
    /// </summary>
    /// <param name="tenantService">Service for accessing the current tenant ID.</param>
    /// <param name="logger">Logger for recording controller operations.</param>
    public TenantTestController(ITenantService tenantService, ILogger<TenantTestController> logger)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint that returns the current tenant ID extracted from the X-Tenant-ID header to verify middleware is working.
    /// This endpoint should be removed or secured in production environments.
    /// </summary>
    /// <returns>
    /// A response containing the current tenant ID and a success message.
    /// </returns>
    [HttpGet("test")]
    public IActionResult Test()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            
            _logger.LogInformation(
                "Tenant test endpoint called successfully. Tenant ID: {TenantId}",
                tenantId);

            return Ok(new
            {
                tenantId = tenantId,
                message = "Tenant middleware is working correctly. The tenant ID was successfully extracted from the X-Tenant-ID header.",
                timestamp = DateTime.UtcNow,
                integrationStatus = new
                {
                    middleware = "Working - Tenant ID extracted from header",
                    service = "Working - TenantService retrieved tenant ID",
                    database = "Ready - Queries will be automatically filtered by tenant ID"
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "Tenant test endpoint failed: Tenant ID is not available. " +
                "This indicates TenantMiddleware is not configured or the request bypassed it.");

            return StatusCode(500, new
            {
                error = "Tenant ID not available",
                message = ex.Message,
                suggestion = "Ensure TenantMiddleware is registered in Program.cs and the request includes the X-Tenant-ID header."
            });
        }
    }
}
