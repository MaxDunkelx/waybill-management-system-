using WaybillManagementSystem.DTOs;
using WaybillManagementSystem.Models;

namespace WaybillManagementSystem.Services;

/// <summary>
/// Interface for job management service.
/// 
/// PURPOSE:
/// This service provides methods for creating, querying, and managing background jobs.
/// It handles job creation, status updates, and result storage.
/// 
/// JOB PROCESSING:
/// Jobs are created with PENDING status and processed by background workers.
/// The service updates job status as jobs are processed (PENDING → PROCESSING → COMPLETED/FAILED).
/// </summary>
public interface IJobService
{
    /// <summary>
    /// Creates a new job and returns the job ID.
    /// 
    /// This method creates a job record in the database with PENDING status.
    /// The job ID is returned immediately to the client, allowing them to poll for status.
    /// </summary>
    /// <param name="jobType">The type of job (e.g., "CSV_IMPORT").</param>
    /// <param name="tenantId">The tenant ID that owns this job.</param>
    /// <param name="inputData">Optional input data for the job (JSON string).</param>
    /// <returns>The created job ID.</returns>
    Task<string> CreateJobAsync(string jobType, string tenantId, string? inputData = null);

    /// <summary>
    /// Gets job information by ID.
    /// 
    /// This method retrieves job status, progress, and results.
    /// Returns null if job doesn't exist or belongs to a different tenant.
    /// </summary>
    /// <param name="jobId">The job ID to retrieve.</param>
    /// <param name="tenantId">The tenant ID (for isolation).</param>
    /// <returns>JobDto if found, null otherwise.</returns>
    Task<JobDto?> GetJobAsync(string jobId, string tenantId);

    /// <summary>
    /// Updates job status to PROCESSING.
    /// 
    /// This method is called by background workers when they start processing a job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="tenantId">The tenant ID (for isolation).</param>
    /// <returns>True if update succeeded, false if job not found.</returns>
    Task<bool> StartJobAsync(string jobId, string tenantId);

    /// <summary>
    /// Updates job status to COMPLETED with results.
    /// 
    /// This method is called by background workers when a job completes successfully.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="tenantId">The tenant ID (for isolation).</param>
    /// <param name="resultData">Job result data (JSON string).</param>
    /// <returns>True if update succeeded, false if job not found.</returns>
    Task<bool> CompleteJobAsync(string jobId, string tenantId, string resultData);

    /// <summary>
    /// Updates job status to FAILED with error message.
    /// 
    /// This method is called by background workers when a job fails.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="tenantId">The tenant ID (for isolation).</param>
    /// <param name="errorMessage">Error message describing the failure.</param>
    /// <returns>True if update succeeded, false if job not found.</returns>
    Task<bool> FailJobAsync(string jobId, string tenantId, string errorMessage);

    /// <summary>
    /// Updates job progress percentage.
    /// 
    /// This method can be called during job processing to update progress.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="tenantId">The tenant ID (for isolation).</param>
    /// <param name="progressPercentage">Progress percentage (0-100).</param>
    /// <returns>True if update succeeded, false if job not found.</returns>
    Task<bool> UpdateProgressAsync(string jobId, string tenantId, int progressPercentage);
}
