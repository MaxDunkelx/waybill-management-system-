using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WaybillManagementSystem.Data;
using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Background service that synchronizes waybills with Priority ERP system.
/// 
/// PURPOSE:
/// This background service periodically processes waybills with PENDING_SYNC status
/// and attempts to synchronize them with the Priority ERP system. It runs continuously
/// in the background, processing waybills asynchronously.
/// 
/// PROCESSING LOGIC:
/// 1. Query waybills with PENDING_SYNC status
/// 2. For each waybill, call ErpIntegrationService.SyncWaybillAsync()
/// 3. Service handles retry logic with exponential backoff
/// 4. Waybill sync status is updated based on result
/// 
/// SCHEDULING:
/// - Processes waybills every 30 seconds
/// - Processes up to 10 waybills per cycle (to prevent overload)
/// - Continues processing even if some waybills fail
/// 
/// TENANT ISOLATION:
/// Background services run outside HTTP context (no X-Tenant-ID header), so the global
/// query filter in ApplicationDbContext cannot determine the current tenant. We use
/// IgnoreQueryFilters() to bypass the tenant filter and process waybills for all tenants.
/// This is safe because:
/// - Background services are internal server processes (no user input)
/// - Waybills already have TenantId set (data isolation maintained)
/// - We're processing internal operations, not exposing data to users
/// - Each waybill's TenantId is preserved during ERP sync operations
/// </summary>
public class ErpSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ErpSyncBackgroundService> _logger;
    private const int ProcessingIntervalSeconds = 30;
    private const int MaxWaybillsPerCycle = 10;

    /// <summary>
    /// Initializes a new instance of the ErpSyncBackgroundService.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <param name="logger">Logger for recording background service operations.</param>
    public ErpSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ErpSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the background service to process waybill ERP synchronization.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for stopping the service.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ERP Sync Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingWaybillsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ERP Sync Background Service");
            }

            // Wait before next processing cycle
            await Task.Delay(TimeSpan.FromSeconds(ProcessingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("ERP Sync Background Service stopped");
    }

    /// <summary>
    /// Processes waybills with PENDING_SYNC status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ProcessPendingWaybillsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var erpService = scope.ServiceProvider.GetRequiredService<IErpIntegrationService>();

        try
        {
            // Query waybills with PENDING_SYNC status for all tenants
            // 
            // CRITICAL: Background services run outside HTTP context (no X-Tenant-ID header),
            // so HttpContext is null and GetCurrentTenantId() returns null. This causes the
            // global query filter in ApplicationDbContext to become "WHERE TenantId = NULL",
            // which matches NO rows, preventing ERP sync from working.
            //
            // SOLUTION: Use IgnoreQueryFilters() to bypass the tenant filter and process waybills
            // for all tenants. This is safe because:
            // 1. Background service runs server-side only (no user input or HTTP requests)
            // 2. Waybills already have TenantId set (data isolation maintained at entity level)
            // 3. We're processing internal operations, not exposing data to users
            // 4. Each waybill's TenantId is preserved and used correctly during sync
            //
            // SECURITY NOTE: This is NOT a security risk because:
            // - Background services are internal server processes
            // - No user can trigger or influence this query
            // - Data is not exposed to users, only processed internally
            // - For HTTP requests (user-facing), we NEVER use IgnoreQueryFilters()
            var pendingWaybills = await dbContext.Waybills
                .IgnoreQueryFilters() // Bypass tenant filter - process waybills for all tenants
                .Where(w => w.ErpSyncStatus == ErpSyncStatus.PendingSync)
                .OrderBy(w => w.CreatedAt) // Process oldest first
                .Take(MaxWaybillsPerCycle)
                .ToListAsync(cancellationToken);

            if (pendingWaybills.Count == 0)
            {
                _logger.LogDebug("No pending waybills to sync with ERP");
                return;
            }

            _logger.LogInformation(
                "Processing {Count} waybills for ERP synchronization",
                pendingWaybills.Count);

            int successCount = 0;
            int failureCount = 0;

            foreach (var waybill in pendingWaybills)
            {
                try
                {
                    // Reload waybill to get latest version (in case it was updated)
                    await dbContext.Entry(waybill).ReloadAsync(cancellationToken);

                    // Attempt to sync with ERP
                    var success = await erpService.SyncWaybillAsync(waybill, cancellationToken);

                    if (success)
                    {
                        successCount++;
                        _logger.LogDebug(
                            "Successfully synced waybill {WaybillId} with ERP",
                            waybill.Id);
                    }
                    else
                    {
                        failureCount++;
                        _logger.LogWarning(
                            "Failed to sync waybill {WaybillId} with ERP after retries",
                            waybill.Id);
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(
                        ex,
                        "Error syncing waybill {WaybillId} with ERP",
                        waybill.Id);
                }
            }

            _logger.LogInformation(
                "ERP sync cycle completed: {SuccessCount} succeeded, {FailureCount} failed",
                successCount,
                failureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending waybills for ERP sync");
        }
    }
}
