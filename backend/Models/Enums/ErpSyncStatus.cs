namespace WaybillManagementSystem.Models.Enums;

/// <summary>
/// Enumeration representing the ERP synchronization status of a waybill.
/// 
/// PURPOSE:
/// This enum tracks the synchronization state of waybills with the Priority ERP system.
/// It enables the system to track which waybills have been successfully synced, which
/// are pending sync, and which have failed to sync.
/// 
/// STATUS VALUES:
/// - PENDING_SYNC: Waybill has not yet been sent to ERP (initial state)
/// - SYNCED: Waybill has been successfully synchronized with ERP
/// - SYNC_FAILED: Waybill synchronization failed (after retries)
/// 
/// USAGE:
/// When a waybill is created or updated, it starts with PENDING_SYNC status.
/// The background ERP sync service attempts to sync waybills with PENDING_SYNC status.
/// On successful sync, status changes to SYNCED. On failure (after retries), status
/// changes to SYNC_FAILED.
/// 
/// RETRY LOGIC:
/// Waybills with PENDING_SYNC status are retried with exponential backoff.
/// After maximum retries, status changes to SYNC_FAILED.
/// </summary>
public enum ErpSyncStatus
{
    /// <summary>
    /// Waybill has not yet been synchronized with the ERP system.
    /// This is the initial state for new waybills.
    /// </summary>
    PendingSync = 0,

    /// <summary>
    /// Waybill has been successfully synchronized with the ERP system.
    /// No further sync attempts are needed.
    /// </summary>
    Synced = 1,

    /// <summary>
    /// Waybill synchronization failed after all retry attempts.
    /// Manual intervention may be required.
    /// </summary>
    SyncFailed = 2
}
