using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WaybillManagementSystem.Data;
using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Background service that processes pending jobs.
/// 
/// PURPOSE:
/// This background service periodically processes jobs with PENDING status
/// and executes them based on job type. It runs continuously in the background.
/// 
/// PROCESSING LOGIC:
/// 1. Query jobs with PENDING status
/// 2. For each job, determine job type and execute appropriate processor
/// 3. Update job status (PENDING → PROCESSING → COMPLETED/FAILED)
/// 4. Store results in job record
/// 
/// JOB TYPES:
/// - CSV_IMPORT: Processes CSV import jobs using WaybillImportService
/// 
/// SCHEDULING:
/// - Processes jobs every 10 seconds
/// - Processes up to 5 jobs per cycle (to prevent overload)
/// - Continues processing even if some jobs fail
/// 
/// TENANT ISOLATION:
/// The service respects tenant isolation by processing jobs within tenant context.
/// However, since this is a background service, it processes jobs for all tenants.
/// </summary>
public class JobProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobProcessorBackgroundService> _logger;
    private const int ProcessingIntervalSeconds = 10;
    private const int MaxJobsPerCycle = 5;

    /// <summary>
    /// Initializes a new instance of the JobProcessorBackgroundService.
    /// </summary>
    public JobProcessorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<JobProcessorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the background service to process pending jobs.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Processor Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Job Processor Background Service");
            }

            await Task.Delay(TimeSpan.FromSeconds(ProcessingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Job Processor Background Service stopped");
    }

    /// <summary>
    /// Processes jobs with PENDING status.
    /// </summary>
    private async Task ProcessPendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jobService = scope.ServiceProvider.GetRequiredService<IJobService>();

        try
        {
            // Query jobs with PENDING status
            // Use IgnoreQueryFilters() because we're in a background service and need to process jobs for all tenants
            var pendingJobs = await dbContext.Jobs
                .IgnoreQueryFilters() // Background service processes jobs for all tenants
                .Where(j => j.Status == JobStatus.Pending)
                .OrderBy(j => j.CreatedAt) // Process oldest first
                .Take(MaxJobsPerCycle)
                .ToListAsync(cancellationToken);

            if (pendingJobs.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Processing {Count} pending jobs", pendingJobs.Count);

            foreach (var job in pendingJobs)
            {
                try
                {
                    // Start job
                    await jobService.StartJobAsync(job.Id, job.TenantId);

                    // Process job based on type
                    string? resultData = null;
                    string? errorMessage = null;

                    switch (job.JobType)
                    {
                        case "CSV_IMPORT":
                            resultData = await ProcessCsvImportJobAsync(job, scope, cancellationToken);
                            if (resultData == null)
                            {
                                errorMessage = "CSV import job failed";
                            }
                            break;

                        default:
                            errorMessage = $"Unknown job type: {job.JobType}";
                            _logger.LogWarning("Unknown job type {JobType} for job {JobId}", job.JobType, job.Id);
                            break;
                    }

                    // Complete or fail job
                    if (errorMessage != null || resultData == null)
                    {
                        await jobService.FailJobAsync(
                            job.Id,
                            job.TenantId,
                            errorMessage ?? "Job processing failed");
                    }
                    else
                    {
                        await jobService.CompleteJobAsync(job.Id, job.TenantId, resultData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing job {JobId}", job.Id);
                    await jobService.FailJobAsync(
                        job.Id,
                        job.TenantId,
                        $"Error processing job: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending jobs");
        }
    }

    /// <summary>
    /// Processes a CSV import job.
    /// </summary>
    private async Task<string?> ProcessCsvImportJobAsync(
        Models.Job job,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        try
        {
            var importService = scope.ServiceProvider.GetRequiredService<IWaybillImportService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Parse input data
            if (string.IsNullOrEmpty(job.InputData))
            {
                return null;
            }

            var inputData = JsonSerializer.Deserialize<Dictionary<string, string>>(job.InputData);
            if (inputData == null || !inputData.ContainsKey("filePath"))
            {
                return null;
            }

            // Read file stream
            var filePath = inputData["filePath"];
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found for CSV import job {JobId}: {FilePath}", job.Id, filePath);
                return null;
            }

            try
            {
                using var fileStream = File.OpenRead(filePath);

                // Process import
                // Note: WaybillImportService will use tenant context from the job's TenantId
                // We need to ensure the DbContext uses the correct tenant for this job
                var result = await importService.ImportFromCsvAsync(fileStream, job.TenantId);

                // Delete temporary file after processing
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary file {FilePath}", filePath);
                }

            // Serialize result
            var resultJson = JsonSerializer.Serialize(new
            {
                totalRows = result.TotalRows,
                successCount = result.SuccessCount,
                errorCount = result.ErrorCount,
                errors = result.Errors,
                warnings = result.Warnings
            });

                return resultJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CSV import job {JobId}", job.Id);
                
                // Delete temporary file even on error
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch
                {
                    // Ignore deletion errors
                }
                
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CSV import job {JobId}", job.Id);
            return null;
        }
    }
}
