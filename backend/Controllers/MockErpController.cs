using Microsoft.AspNetCore.Mvc;

namespace WaybillManagementSystem.Controllers;

/// <summary>
/// Mock Priority ERP endpoint for testing ERP integration.
/// 
/// PURPOSE:
/// This controller simulates a Priority ERP system endpoint that accepts waybill data.
/// It is used for testing the ERP integration service without requiring a real ERP system.
/// 
/// BEHAVIOR:
/// - Accepts waybill data via HTTP POST
/// - Simulates 10% random failure rate (to test retry logic)
/// - Returns success/failure responses
/// - Logs all sync attempts for debugging
/// 
/// USAGE:
/// This endpoint is called by ErpIntegrationService when syncing waybills.
/// The endpoint URL is configured in appsettings.json (ErpIntegration:EndpointUrl).
/// 
/// NOTE:
/// This is a mock endpoint for development/testing. In production, this would be
/// replaced with actual Priority ERP API endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MockErpController : ControllerBase
{
    private readonly ILogger<MockErpController> _logger;
    private static readonly Random _random = new Random();

    /// <summary>
    /// Initializes a new instance of the MockErpController.
    /// </summary>
    /// <param name="logger">Logger for recording ERP sync operations.</param>
    public MockErpController(ILogger<MockErpController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Mock endpoint that simulates Priority ERP waybill synchronization.
    /// 
    /// This endpoint simulates real ERP behavior:
    /// - Accepts waybill data
    /// - Simulates 10% random failure rate
    /// - Returns appropriate HTTP status codes
    /// 
    /// FAILURE SIMULATION:
    /// The endpoint randomly returns 500 Internal Server Error approximately 10% of the time
    /// to simulate real-world ERP failures and test retry logic.
    /// </summary>
    /// <param name="waybillData">The waybill data to synchronize.</param>
    /// <returns>
    /// Status 200 OK if sync succeeds.
    /// Status 500 Internal Server Error if sync fails (simulated 10% failure rate).
    /// </returns>
    [HttpPost("sync-waybill")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult SyncWaybill([FromBody] object waybillData)
    {
        if (waybillData == null)
        {
            _logger.LogWarning("Mock ERP received null waybill data");
            return BadRequest(new { error = "Waybill data is required" });
        }

        // Simulate 10% random failure rate
        var shouldFail = _random.Next(1, 101) <= 10;

        if (shouldFail)
        {
            _logger.LogWarning(
                "Mock ERP simulating failure for waybill sync. " +
                "This tests the retry logic with exponential backoff.");
            
            return StatusCode(500, new
            {
                error = "ERP synchronization failed",
                message = "Simulated ERP failure (10% failure rate for testing retry logic)",
                timestamp = DateTime.UtcNow
            });
        }

        // Success case
        _logger.LogInformation(
            "Mock ERP successfully processed waybill sync. " +
            "In production, this would sync with actual Priority ERP system.");

        return Ok(new
        {
            success = true,
            message = "Waybill synchronized successfully with Priority ERP",
            timestamp = DateTime.UtcNow
        });
    }
}
