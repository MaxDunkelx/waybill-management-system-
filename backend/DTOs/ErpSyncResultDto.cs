namespace WaybillManagementSystem.DTOs;

/// <summary>
/// Data Transfer Object for ERP synchronization result.
/// 
/// PURPOSE:
/// This DTO represents the result of synchronizing a waybill with the Priority ERP system.
/// It includes information about the sync attempt, success/failure status, and any error details.
/// 
/// USAGE:
/// This DTO is used to report sync results in logs and potentially in API responses
/// for monitoring and debugging purposes.
/// </summary>
public class ErpSyncResultDto
{
    /// <summary>
    /// The waybill ID that was synchronized.
    /// </summary>
    public string WaybillId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the synchronization was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The number of retry attempts made before success or failure.
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Error message if synchronization failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when the synchronization was completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// The final sync status after the synchronization attempt.
    /// </summary>
    public Models.Enums.ErpSyncStatus FinalStatus { get; set; }
}
