using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for job information.
/// 
/// PURPOSE:
/// This DTO represents job information returned to clients when querying job status.
/// It includes job status, progress, results, and error information.
/// 
/// USAGE:
/// This DTO is used in GET /api/jobs/{id} endpoint to return job status to clients.
/// </summary>
public class JobDto
{
    /// <summary>
    /// The job ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The type of job (e.g., "CSV_IMPORT").
    /// </summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the job.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Job result data (JSON string, parsed as object if needed).
    /// This contains the job results (e.g., import statistics).
    /// </summary>
    public string? ResultData { get; set; }

    /// <summary>
    /// Error message if job failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the job started processing.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the job completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
