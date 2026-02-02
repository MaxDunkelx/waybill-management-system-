namespace WaybillManagementSystem.Models.Enums;

/// <summary>
/// Enumeration representing the status of a background job.
/// 
/// PURPOSE:
/// This enum tracks the lifecycle of background jobs (e.g., CSV import jobs).
/// It enables the system to track which jobs are pending, processing, completed, or failed.
/// 
/// STATUS VALUES:
/// - PENDING: Job has been created but not yet started
/// - PROCESSING: Job is currently being processed
/// - COMPLETED: Job completed successfully
/// - FAILED: Job failed during processing
/// 
/// USAGE:
/// When a job is created, it starts with PENDING status.
/// The background job processor picks up PENDING jobs and changes status to PROCESSING.
/// On completion, status changes to COMPLETED or FAILED.
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job has been created but not yet started.
    /// This is the initial state for new jobs.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job is currently being processed by a background worker.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job failed during processing.
    /// </summary>
    Failed = 3
}
