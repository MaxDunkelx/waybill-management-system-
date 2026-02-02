using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WaybillManagementSystem.Models.Enums;

namespace WaybillManagementSystem.Models;

/// <summary>
/// Represents a background job in the system.
/// 
/// PURPOSE:
/// This entity tracks background jobs (e.g., CSV import jobs) that are processed
/// asynchronously. It stores job status, results, and metadata for monitoring and
/// client polling.
/// 
/// JOB LIFECYCLE:
/// 1. Job created with PENDING status → Job ID returned to client immediately
/// 2. Background worker picks up job → Status changes to PROCESSING
/// 3. Job completes → Status changes to COMPLETED or FAILED
/// 4. Client polls GET /api/jobs/{id} to check status
/// 
/// TENANT ISOLATION:
/// Jobs are tenant-scoped to ensure tenants can only see their own jobs.
/// The global query filter in ApplicationDbContext automatically filters jobs by TenantId.
/// </summary>
public class Job
{
    /// <summary>
    /// Unique job identifier (GUID).
    /// This is the primary key and is returned to clients when a job is created.
    /// </summary>
    [Key]
    [Required]
    [StringLength(50)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The type of job (e.g., "CSV_IMPORT", "REPORT_GENERATION").
    /// This field allows the system to handle different job types with different processors.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the job.
    /// </summary>
    [Required]
    public JobStatus Status { get; set; } = JobStatus.Pending;

    /// <summary>
    /// Foreign key reference to the Tenant that owns this job.
    /// This field is required and ensures that every job belongs to exactly one tenant.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Job input data (JSON string).
    /// This field stores the job parameters (e.g., file path, import options).
    /// Stored as JSON for flexibility.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? InputData { get; set; }

    /// <summary>
    /// Job result data (JSON string).
    /// This field stores the job results (e.g., import statistics, error details).
    /// Stored as JSON for flexibility.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ResultData { get; set; }

    /// <summary>
    /// Error message if job failed.
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Progress percentage (0-100).
    /// This field can be updated during job processing to show progress.
    /// </summary>
    public int ProgressPercentage { get; set; } = 0;

    /// <summary>
    /// Timestamp when the job was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the job started processing.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the job completed (successfully or with failure).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Navigation property to the Tenant that owns this job.
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Tenant Tenant { get; set; } = null!;
}
