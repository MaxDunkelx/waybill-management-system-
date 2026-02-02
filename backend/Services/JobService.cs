using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WaybillManagementSystem.Data;
using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Models;
using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Service implementation for job management.
/// 
/// IMPLEMENTATION DETAILS:
/// This service manages background jobs, including creation, status updates, and result storage.
/// Jobs are stored in the database and processed by background workers.
/// 
/// JOB LIFECYCLE:
/// 1. CreateJobAsync: Creates job with PENDING status → Returns job ID
/// 2. StartJobAsync: Background worker starts job → Status → PROCESSING
/// 3. CompleteJobAsync/FailJobAsync: Job finishes → Status → COMPLETED/FAILED
/// 4. GetJobAsync: Client polls for status
/// 
/// TENANT ISOLATION:
/// All job operations are tenant-scoped. The global query filter ensures tenants
/// can only access their own jobs.
/// </summary>
public class JobService : IJobService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<JobService> _logger;

    /// <summary>
    /// Initializes a new instance of the JobService.
    /// </summary>
    /// <param name="dbContext">Database context for job operations.</param>
    /// <param name="logger">Logger for recording job operations.</param>
    public JobService(
        ApplicationDbContext dbContext,
        ILogger<JobService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new job and returns the job ID.
    /// </summary>
    public async Task<string> CreateJobAsync(string jobType, string tenantId, string? inputData = null)
    {
        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            JobType = jobType,
            Status = JobStatus.Pending,
            TenantId = tenantId,
            InputData = inputData,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Jobs.Add(job);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created job {JobId} of type {JobType} for tenant {TenantId}",
            job.Id,
            jobType,
            tenantId);

        return job.Id;
    }

    /// <summary>
    /// Gets job information by ID.
    /// </summary>
    public async Task<JobDto?> GetJobAsync(string jobId, string tenantId)
    {
        var job = await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId);

        if (job == null)
        {
            return null;
        }

        return new JobDto
        {
            Id = job.Id,
            JobType = job.JobType,
            Status = job.Status,
            ProgressPercentage = job.ProgressPercentage,
            ResultData = job.ResultData,
            ErrorMessage = job.ErrorMessage,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt
        };
    }

    /// <summary>
    /// Updates job status to PROCESSING.
    /// </summary>
    public async Task<bool> StartJobAsync(string jobId, string tenantId)
    {
        var job = await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId);

        if (job == null)
        {
            return false;
        }

        job.Status = JobStatus.Processing;
        job.StartedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Started job {JobId} for tenant {TenantId}", jobId, tenantId);
        return true;
    }

    /// <summary>
    /// Updates job status to COMPLETED with results.
    /// </summary>
    public async Task<bool> CompleteJobAsync(string jobId, string tenantId, string resultData)
    {
        var job = await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId);

        if (job == null)
        {
            return false;
        }

        job.Status = JobStatus.Completed;
        job.ResultData = resultData;
        job.ProgressPercentage = 100;
        job.CompletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Completed job {JobId} for tenant {TenantId}", jobId, tenantId);
        return true;
    }

    /// <summary>
    /// Updates job status to FAILED with error message.
    /// </summary>
    public async Task<bool> FailJobAsync(string jobId, string tenantId, string errorMessage)
    {
        var job = await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId);

        if (job == null)
        {
            return false;
        }

        job.Status = JobStatus.Failed;
        job.ErrorMessage = errorMessage;
        job.CompletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogWarning("Failed job {JobId} for tenant {TenantId}: {ErrorMessage}", jobId, tenantId, errorMessage);
        return true;
    }

    /// <summary>
    /// Updates job progress percentage.
    /// </summary>
    public async Task<bool> UpdateProgressAsync(string jobId, string tenantId, int progressPercentage)
    {
        var job = await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.TenantId == tenantId);

        if (job == null)
        {
            return false;
        }

        job.ProgressPercentage = Math.Clamp(progressPercentage, 0, 100);
        await _dbContext.SaveChangesAsync();

        return true;
    }
}
